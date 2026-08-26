#!/usr/bin/env python3
"""
FrontendVerificationTool - Scans frontend code for hardcoded state values
and business logic in components. Verifies data flows from stores/API only.

Usage: python verify_frontend.py <frontend_src_dir>
"""

import os
import re
import sys
import json
from pathlib import Path
from collections import defaultdict

SUSPICIOUS_PATTERNS = [
    (r'(?:const|let|var)\s+\w+\s*=\s*\{[^}]*temperature\s*:\s*\d+', 'Hardcoded temperature value'),
    (r'(?:const|let|var)\s+\w+\s*=\s*\{[^}]*usage\s*:\s*\d+', 'Hardcoded usage value'),
    (r'(?:const|let|var)\s+\w+\s*=\s*\{[^}]*power\w*\s*:\s*\d+', 'Hardcoded power value'),
    (r'(?:const|let|var)\s+\w+\s*=\s*\{[^}]*clock\w*\s*:\s*\d+', 'Hardcoded clock value'),
    (r'(?:const|let|var)\s+\w+\s*=\s*\{[^}]*fan\w*\s*:\s*\d+', 'Hardcoded fan value'),
    (r'(?:const|let|var)\s+\w+\s*=\s*\{[^}]*battery\w*\s*:\s*\d+', 'Hardcoded battery value'),
    (r'(?:const|let|var)\s+\w+\s*=\s*\{[^}]*charge\w*\s*:\s*\d+', 'Hardcoded charge value'),
]

BUSINESS_LOGIC_PATTERNS = [
    (r'\bif\s*\([^)]*(?:temperature|temp|usage|power|clock|fan)\w*\s*[<>]=?\s*\d+',
     'Business logic: conditional on hardware value'),
    (r'\bswitch\s*\([^)]*(?:temperature|temp|usage|power|clock)\w*',
     'Business logic: switch on hardware value'),
    (r'(?:temperature|temp|usage|power|clock)\w*\s*[+\-*/]\s*\d+',
     'Business logic: arithmetic on hardware value'),
]

STORE_IMPORT_PATTERN = re.compile(
    r'import\s+\{?\s*(\w+)\s*\}?\s+from\s+[\'"]@/store/(\w+)[\'"]'
)

API_CALL_PATTERN = re.compile(
    r'(?:api|hardwareApi|performanceApi|mlpApi|auraApi|bindingApi)\.\w+'
)

USESIGNALR_PATTERN = re.compile(
    r'useSignalR\(\)'
)

INLINE_DATA_PATTERN = re.compile(
    r'(?:const|let|var)\s+\w+(?:State|Data|Config|Result)\s*=\s*('
    r'\{[^}]*\}|new\s+Array|\[[^\]]*\])'
)


class FrontendVerifier:
    def __init__(self, src_dir: str):
        self.src_dir = Path(src_dir)
        self.findings = []
        self.passed = []
        self.warnings = []
        self.files_scanned = 0
        self.components_checked = 0
        self.store_imports = defaultdict(set)
        self.api_usage = defaultdict(list)
        self.signalr_usage = []

    def scan(self) -> dict:
        if not self.src_dir.exists():
            return {"error": f"Directory not found: {self.src_dir}"}

        tsx_files = list(self.src_dir.rglob('*.tsx')) + list(self.src_dir.rglob('*.ts'))

        for file_path in sorted(tsx_files):
            if 'node_modules' in str(file_path):
                continue
            self._check_file(file_path)

        self._verify_data_flow()
        return self._generate_report()

    def _check_file(self, file_path: Path):
        self.files_scanned += 1
        content = file_path.read_text(encoding='utf-8')
        rel_path = str(file_path.relative_to(self.src_dir))

        if '/pages/' in str(file_path).replace('\\', '/') or '/components/' in str(file_path).replace('\\', '/'):
            self.components_checked += 1
            self._check_component(rel_path, content)

        self._check_for_hardcoded_values(rel_path, content)
        self._check_for_business_logic(rel_path, content)
        self._check_data_flow(rel_path, content)

    def _check_component(self, path: str, content: str):
        has_store = bool(re.search(r'useHardwareStore|useMlpStore|useSettingsStore', content))
        has_signalr = bool(USESIGNALR_PATTERN.search(content))
        has_api = bool(API_CALL_PATTERN.search(content))

        if has_store or has_api or has_signalr:
            self.passed.append(f"[OK] {path}: Component uses data sources (store={has_store}, api={has_api}, signalR={has_signalr})")
        elif 'useState' in content or 'useEffect' in content:
            self.warnings.append(f"[WARN] {path}: Component has state/hooks but no store/API import")

    def _check_for_hardcoded_values(self, path: str, content: str):
        for pattern, description in SUSPICIOUS_PATTERNS:
            matches = re.findall(pattern, content, re.IGNORECASE)
            for match in matches:
                if 'default' in match.lower() or 'initial' in match.lower() or 'fallback' in match.lower():
                    self.warnings.append(
                        f"[INFO] {path}: {description} (appears to be default/fallback: {match.strip()[:100]})"
                    )
                else:
                    self.findings.append(
                        f"[FAIL] {path}: {description}: {match.strip()[:100]}"
                    )

    def _check_for_business_logic(self, path: str, content: str):
        for pattern, description in BUSINESS_LOGIC_PATTERNS:
            matches = re.findall(pattern, content, re.IGNORECASE)
            for match in matches:
                if '/pages/' in path or '/components/' in path:
                    self.findings.append(
                        f"[FAIL] {path}: {description}: {match.strip()[:100]}"
                    )

    def _check_data_flow(self, path: str, content: str):
        for match in STORE_IMPORT_PATTERN.finditer(content):
            store_name = match.group(2)
            self.store_imports[store_name].add(path)

        for match in API_CALL_PATTERN.finditer(content):
            self.api_usage[path].append(match.group(0))

        if 'useSignalR' in content or 'signalRService' in content:
            self.signalr_usage.append(path)

    def _verify_data_flow(self):
        for store_name, files in self.store_imports.items():
            self.passed.append(
                f"[DATAFLOW] Store '{store_name}' imported in {len(files)} files: {', '.join(sorted(files)[:5])}"
            )

        if self.signalr_usage:
            self.passed.append(
                f"[SIGNALR] SignalR used in {len(self.signalr_usage)} files"
            )
        else:
            self.findings.append(
                "[FAIL] No SignalR usage found in any frontend file"
            )

        has_api_usage = len(self.api_usage) > 0
        if has_api_usage:
            self.passed.append(
                f"[API] API services used in {len(self.api_usage)} files"
            )
        else:
            self.warnings.append(
                "[WARN] No API service usage found"
            )

    def _generate_report(self) -> dict:
        return {
            "summary": {
                "files_scanned": self.files_scanned,
                "components_checked": self.components_checked,
                "findings": len(self.findings),
                "warnings": len(self.warnings),
                "passed": len(self.passed),
                "total_checks": len(self.findings) + len(self.warnings) + len(self.passed),
            },
            "results": {
                "failures": self.findings,
                "warnings": self.warnings,
                "passes": self.passed,
            },
            "data_flow": {
                "store_imports": {k: sorted(v) for k, v in self.store_imports.items()},
                "signalr_usage": self.signalr_usage,
                "api_usage_count": len(self.api_usage),
            },
            "verdict": "PASS" if len(self.findings) == 0 else "FAIL",
        }


def main():
    if len(sys.argv) < 2:
        print("Usage: python verify_frontend.py <frontend_src_dir>")
        print("Example: python verify_frontend.py /workspace/ZTR_OS/frontend/src")
        sys.exit(1)

    src_dir = sys.argv[1]
    verifier = FrontendVerifier(src_dir)
    report = verifier.scan()

    print("\n" + "=" * 70)
    print("  ZTR_OS FRONTEND VERIFICATION REPORT")
    print("=" * 70)

    s = report["summary"]
    print(f"\n  Files scanned:        {s['files_scanned']}")
    print(f"  Components checked:   {s['components_checked']}")
    print(f"  Passes:               {s['passed']}")
    print(f"  Warnings:             {s['warnings']}")
    print(f"  Failures:             {s['findings']}")

    if report["results"]["failures"]:
        print("\n  --- FAILURES ---")
        for f in report["results"]["failures"]:
            print(f"  ❌ {f}")

    if report["results"]["warnings"]:
        print("\n  --- WARNINGS ---")
        for w in report["results"]["warnings"]:
            print(f"  ⚠ {w}")

    print(f"\n  Verdict: {report['verdict']}")
    print("=" * 70)

    if report["verdict"] == "PASS":
        print("  ✅ All checks passed!")
    else:
        print("  ❌ Some checks failed. See report for details.")

    output_path = Path(src_dir).parent.parent / "frontend_verification_report.json"
    with open(output_path, 'w') as f:
        json.dump(report, f, indent=2)
    print(f"\n  Report saved to: {output_path}")

    sys.exit(0 if report["verdict"] == "PASS" else 1)


if __name__ == "__main__":
    main()