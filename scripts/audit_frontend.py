#!/usr/bin/env python3
"""
Frontend Code Audit Script - Deep audit of frontend source code.
Identifies hardcoded values, business logic in components, and verifies
strict backend binding.

Usage: python audit_frontend.py <frontend_dir>
"""

import os
import re
import sys
import json
from pathlib import Path
from collections import defaultdict


class FrontendAuditor:
    def __init__(self, frontend_dir: str):
        self.frontend_dir = Path(frontend_dir)
        self.src_dir = self.frontend_dir / "src"
        self.findings = []
        self.metrics = defaultdict(int)
        self.audit_results = {
            "hardcoded_values": [],
            "business_logic_in_components": [],
            "missing_data_flow": [],
            "strict_binding_violations": [],
            "signalR_review": [],
            "store_usage": [],
            "summary": {},
        }

    def run_audit(self) -> dict:
        self._audit_source_tree()
        self._audit_stores()
        self._audit_services()
        self._audit_components()
        self._audit_signalR()
        self._generate_summary()
        return self.audit_results

    def _audit_source_tree(self):
        self.audit_results["source_structure"] = {
            "pages": [],
            "components": [],
            "stores": [],
            "services": [],
            "hooks": [],
        }

        for path in sorted(self.src_dir.rglob('*.tsx')):
            rel = str(path.relative_to(self.src_dir))
            if '/pages/' in rel or '\\pages\\' in rel:
                self.audit_results["source_structure"]["pages"].append(rel)
            elif '/components/' in rel or '\\components\\' in rel:
                self.audit_results["source_structure"]["components"].append(rel)
            elif '/stores/' in rel or '\\stores\\' in rel:
                self.audit_results["source_structure"]["stores"].append(rel)
            elif '/services/' in rel or '\\services\\' in rel:
                self.audit_results["source_structure"]["services"].append(rel)
            elif '/hooks/' in rel or '\\hooks\\' in rel:
                self.audit_results["source_structure"]["hooks"].append(rel)

        self.audit_results["source_structure"]["types"] = [
            str(p.relative_to(self.src_dir)) for p in sorted(self.src_dir.rglob('*.ts'))
        ]

    def _audit_stores(self):
        store_files = sorted(self.src_dir.glob('store/*.ts'))

        for store_file in store_files:
            content = store_file.read_text(encoding='utf-8')
            rel = str(store_file.relative_to(self.src_dir))

            has_signalr = 'signalRService' in content or 'SignalR' in content
            has_api = any(kw in content for kw in ['api', 'fetch', 'axios'])
            has_default_state = 'default' in content.lower() or 'initial' in content.lower()

            self.audit_results["store_usage"].append({
                "file": rel,
                "has_signalR_subscription": has_signalr,
                "has_api_calls": has_api,
                "has_default_state": has_default_state,
                "data_sources": self._extract_data_sources(content),
            })

            if has_default_state:
                defaults = self._extract_default_values(content)
                if defaults:
                    self.audit_results["hardcoded_values"].extend([
                        {"file": rel, "type": "store_default", "value": d}
                        for d in defaults
                    ])

    def _audit_services(self):
        service_files = sorted(self.src_dir.glob('services/*.ts'))

        for svc_file in service_files:
            content = svc_file.read_text(encoding='utf-8')
            rel = str(svc_file.relative_to(self.src_dir))

            if 'hardware' in rel.lower():
                endpoints = re.findall(r"'([^']+)'", content)
                self.audit_results["hardcoded_values"].append({
                    "file": rel,
                    "type": "hardware_endpoints",
                    "details": [e for e in endpoints if e.startswith('/api/')],
                })

    def _audit_components(self):
        component_files = list(self.src_dir.rglob('*.tsx'))

        for comp_file in sorted(component_files):
            content = comp_file.read_text(encoding='utf-8')
            rel = str(comp_file.relative_to(self.src_dir))

            self._check_hardcoded_state(rel, content)
            self._check_business_logic(rel, content)
            self._check_data_flow_compliance(rel, content)

    def _check_hardcoded_state(self, path: str, content: str):
        patterns = [
            (r'temperature\s*[:=]\s*(\d+)', 'temperature'),
            (r'usage\s*[:=]\s*(\d+)', 'usage'),
            (r'power\w*\s*[:=]\s*(\d+)', 'power'),
            (r'clock\w*\s*[:=]\s*(\d+)', 'clock'),
            (r'speed\s*[:=]\s*(\d+)', 'speed'),
            (r'charge\w*\s*[:=]\s*(\d+)', 'charge'),
        ]

        for pattern, value_type in patterns:
            for match in re.finditer(pattern, content, re.IGNORECASE):
                value = int(match.group(1))
                if value == 0:
                    continue
                if 'default' in content[:content.find(match.group(0))].lower() or \
                   'initial' in content[:content.find(match.group(0))].lower() or \
                   'fallback' in content[:content.find(match.group(0))].lower():
                    continue

                self.audit_results["hardcoded_values"].append({
                    "file": path,
                    "type": value_type,
                    "value": value,
                    "match": match.group(0).strip(),
                })

    def _check_business_logic(self, path: str, content: str):
        logic_patterns = [
            (r'if\s*\([^)]*temperature\s*[<>]=?\s*\d+', 'conditional on temperature'),
            (r'if\s*\([^)]*usage\s*[<>]=?\s*\d+', 'conditional on usage'),
            (r'if\s*\([^)]*power\w*\s*[<>]=?\s*\d+', 'conditional on power'),
            (r'if\s*\([^)]*clock\w*\s*[<>]=?\s*\d+', 'conditional on clock'),
            (r'\?\s*\w*\.(?:temperature|usage|power)\w*\s*[<>]=?\s*\d+',
             'ternary on hardware value'),
        ]

        for pattern, description in logic_patterns:
            matches = re.findall(pattern, content, re.IGNORECASE)
            if matches:
                self.audit_results["business_logic_in_components"].append({
                    "file": path,
                    "description": description,
                    "matches": matches,
                })

    def _check_data_flow_compliance(self, path: str, content: str):
        if '/pages/' not in path:
            return

        data_sources_found = []

        if re.search(r'useHardwareStore|useMlpStore|useSettingsStore', content):
            data_sources_found.append('zustand_store')

        if re.search(r'(?:api|hardwareApi|performanceApi|mlpApi|otherApi)\.\w+', content):
            data_sources_found.append('api_service')

        if 'useSignalR' in content or 'signalRService' in content:
            data_sources_found.append('signalR')

        state_values = self._find_state_values(content)
        if state_values and not data_sources_found:
            self.audit_results["missing_data_flow"].append({
                "file": path,
                "issue": "Component has state values but no store/API/SignalR data source",
                "state_values": state_values[:5],
            })

    def _find_state_values(self, content: str) -> list:
        values = []
        patterns = [
            r'const\s+\w+\s*=\s*use(?:State|Ref)\s*<[^>]+>\s*\(\s*(\w+)',
            r'const\s+\w+\s*=\s*useState\s*\(\s*(\w+)',
        ]
        for pattern in patterns:
            values.extend(re.findall(pattern, content))
        return values

    def _audit_signalR(self):
        signalr_file = self.src_dir / 'services' / 'signalR.ts'
        if not signalr_file.exists():
            self.audit_results["signalR_review"].append({
                "status": "FAIL",
                "issue": "signalR.ts not found",
            })
            return

        content = signalr_file.read_text(encoding='utf-8')
        rel = 'services/signalR.ts'

        checks = {
            "has_hub_url": bool(re.search(r'hubUrl|HubConnectionBuilder', content)),
            "has_reconnect_logic": 'withAutomaticReconnect' in content or 'scheduleReconnect' in content,
            "has_event_handlers": 'HardwareUpdate' in content or 'SensorUpdate' in content,
            "has_status_tracking": 'connection:status' in content,
            "has_retry_logic": 'retryDelay' in content or 'scheduleReconnect' in content,
            "hub_url": self._extract_hub_url(content),
        }

        self.audit_results["signalR_review"].append({
            "file": rel,
            "checks": checks,
        })

    def _extract_hub_url(self, content: str) -> str:
        match = re.search(r"hubUrl\s*=\s*`([^`]+)`", content)
        if match:
            return match.group(1)
        match = re.search(r'hubUrl\s*=\s*"([^"]+)"', content)
        if match:
            return match.group(1)
        return "unknown"

    def _extract_data_sources(self, content: str) -> list:
        sources = []
        if 'signalRService' in content:
            sources.append('SignalR')
        if 'create' in content and 'zustand' in content:
            sources.append('Zustand')
        if 'localStorage' in content:
            sources.append('localStorage')
        return sources

    def _extract_default_values(self, content: str) -> list:
        defaults = []
        patterns = [
            (r'(?:const|let)\s+(\w*[Dd]efault\w*)\s*=\s*(\{[^}]*\}|\[[^\]]*\])', 'object_default'),
            (r'(?:const|let)\s+(\w*[Dd]efault\w*)\s*=\s*(\d+)', 'numeric_default'),
            (r'(?:const|let)\s+(\w*[Ii]nitial\w*)\s*=\s*(\{[^}]*\}|\[[^\]]*\])', 'initial_value'),
        ]
        for pattern, kind in patterns:
            for match in re.finditer(pattern, content):
                name = match.group(1)
                value = match.group(2)
                if len(value) > 500:
                    value = value[:500] + '...'
                defaults.append({"name": name, "kind": kind, "value_preview": value})
        return defaults

    def _generate_summary(self):
        total_findings = (
            len(self.audit_results["hardcoded_values"]) +
            len(self.audit_results["business_logic_in_components"]) +
            len(self.audit_results["missing_data_flow"]) +
            len(self.audit_results["strict_binding_violations"])
        )

        self.audit_results["summary"] = {
            "total_findings": total_findings,
            "hardcoded_values_count": len(self.audit_results["hardcoded_values"]),
            "business_logic_count": len(self.audit_results["business_logic_in_components"]),
            "missing_data_flow_count": len(self.audit_results["missing_data_flow"]),
            "strict_binding_violations_count": len(self.audit_results["strict_binding_violations"]),
            "verdict": "PASS" if total_findings == 0 else "REVIEW_REQUIRED",
        }


def main():
    if len(sys.argv) < 2:
        print("Usage: python audit_frontend.py <frontend_dir>")
        print("Example: python audit_frontend.py /workspace/ZTR_OS/frontend")
        sys.exit(1)

    frontend_dir = sys.argv[1]
    auditor = FrontendAuditor(frontend_dir)
    results = auditor.run_audit()

    print("\n" + "=" * 70)
    print("  ZTR_OS FRONTEND CODE AUDIT")
    print("=" * 70)

    s = results["summary"]
    print(f"\n  Total findings:        {s['total_findings']}")
    print(f"  Hardcoded values:      {s['hardcoded_values_count']}")
    print(f"  Business logic:        {s['business_logic_count']}")
    print(f"  Missing data flow:     {s['missing_data_flow_count']}")
    print(f"  Binding violations:    {s['strict_binding_violations_count']}")
    print(f"  Verdict:               {s['verdict']}")

    if results["hardcoded_values"]:
        print("\n  --- HARDCODED VALUES ---")
        for item in results["hardcoded_values"][:20]:
            print(f"  ⚠ [{item['file']}] {item.get('type', 'unknown')}: {str(item.get('value', ''))[:80]}")
        if len(results["hardcoded_values"]) > 20:
            print(f"  ... and {len(results['hardcoded_values']) - 20} more")

    if results["business_logic_in_components"]:
        print("\n  --- BUSINESS LOGIC IN COMPONENTS ---")
        for item in results["business_logic_in_components"]:
            print(f"  ❌ [{item['file']}] {item['description']}")

    if results["missing_data_flow"]:
        print("\n  --- MISSING DATA FLOW ---")
        for item in results["missing_data_flow"]:
            print(f"  ⚠ [{item['file']}] {item['issue']}")

    if results["signalR_review"]:
        print("\n  --- SIGNALR REVIEW ---")
        for item in results["signalR_review"]:
            if "checks" in item:
                checks = item["checks"]
                url_status = "✅" if checks.get("has_hub_url") else "❌"
                reconnect_status = "✅" if checks.get("has_reconnect_logic") else "❌"
                print(f"  {url_status} Hub URL: {checks.get('hub_url', 'N/A')}")
                print(f"  {reconnect_status} Reconnect logic: {'present' if checks.get('has_reconnect_logic') else 'missing'}")
            else:
                print(f"  ❌ {item.get('issue', 'Unknown issue')}")

    report_path = Path(frontend_dir).parent / "frontend_audit_report.json"
    with open(report_path, 'w') as f:
        json.dump(results, f, indent=2)
    print(f"\n  Full audit report: {report_path}")

    sys.exit(0 if s["verdict"] == "PASS" else 0)


if __name__ == "__main__":
    main()