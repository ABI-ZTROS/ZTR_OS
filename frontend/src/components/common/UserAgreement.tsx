import { useState, useEffect, useRef, useCallback } from 'react'
import { useRipple } from '@/hooks/useRipple'

interface UserAgreementProps {
  open: boolean
  onAgree: () => void
  onDisagree: () => void
  currentVersion?: string
}

const AGREEMENT_VERSION = '3.0.0'
const COUNTDOWN_SECONDS = 120

export function UserAgreement({
  open,
  onAgree,
  onDisagree,
  currentVersion = AGREEMENT_VERSION,
}: UserAgreementProps) {
  const [countdown, setCountdown] = useState(COUNTDOWN_SECONDS)
  const [canAgree, setCanAgree] = useState(false)
  const [hasScrolledToBottom, setHasScrolledToBottom] = useState(false)
  const contentRef = useRef<HTMLDivElement>(null)
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const ripple = useRipple()

  useEffect(() => {
    if (!open) return
    setCountdown(COUNTDOWN_SECONDS)
    setCanAgree(false)
    setHasScrolledToBottom(false)
    if (contentRef.current) contentRef.current.scrollTop = 0

    timerRef.current = setInterval(() => {
      setCountdown((prev) => {
        if (prev <= 1) {
          if (timerRef.current) clearInterval(timerRef.current)
          return 0
        }
        return prev - 1
      })
    }, 1000)

    return () => {
      if (timerRef.current) clearInterval(timerRef.current)
    }
  }, [open])

  useEffect(() => {
    if (countdown === 0 && hasScrolledToBottom) {
      setCanAgree(true)
    }
  }, [countdown, hasScrolledToBottom])

  const handleScroll = useCallback(() => {
    const el = contentRef.current
    if (!el) return
    const atBottom = el.scrollTop + el.clientHeight >= el.scrollHeight - 20
    if (atBottom) setHasScrolledToBottom(true)
  }, [])

  if (!open) return null

  const formatTime = (s: number) => {
    const m = Math.floor(s / 60)
    const sec = s % 60
    return `${m.toString().padStart(2, '0')}:${sec.toString().padStart(2, '0')}`
  }

  return (
    <div className="agreement-overlay">
      <div className="agreement-window">
        <div className="agreement-header">
          <div className="agreement-header-icon">⚠</div>
          <div>
            <h2>ZTR_OS 用户协议与法律声明</h2>
            <p className="agreement-subtitle">
              请务必仔细阅读协议（剩余 {formatTime(countdown)}）
            </p>
            <p className="agreement-hint">
              请滚动至协议最底部（拖动速度已被限制，请耐心阅读）
            </p>
          </div>
        </div>

        <div
          ref={contentRef}
          className="agreement-content"
          onScroll={handleScroll}
        >
          <div className="agreement-section">
            <h3>一、协议前言与术语定义</h3>
            <p>
              欢迎使用 ZTR_OS（以下简称"本软件"）。本协议为软件开发者（以下简称"甲方"）与您（以下简称"乙方"或"用户"）之间就本软件的下载、安装、复制、使用、运行及衍生行为所订立的具有法律约束力的最终用户许可协议（End-User License Agreement，简称"EULA"）。
            </p>
            <p>
              乙方在下载、安装、运行本软件或以任何方式使用本软件之任何功能前，应当完整阅读本协议全部条款。乙方点击"同意并继续"按钮、或实际安装、运行、使用本软件，均视为乙方已充分阅读、理解并毫无保留地接受本协议全部内容，本协议即对乙方发生法律效力。
            </p>
            <p>
              若乙方不同意本协议之任何条款，应当立即停止下载、安装、使用本软件，并删除已复制之本软件及其全部副本。乙方继续使用本软件即构成对本协议的持续确认与接受。
            </p>
            <p>
              本软件依据中华人民共和国相关法律法规开发、发布与运营，所有用户均须遵守中华人民共和国法律、行政法规、地方性法规、部门规章及司法解释的规定。
            </p>
            <p className="law-ref">
              法律依据：《中华人民共和国民法典》第四百九十一条（电子合同成立）、《中华人民共和国著作权法》第十条（著作权内容）。
            </p>

            <div className="warning-box">
              <strong>[警告] 重要声明：本软件不是开源软件</strong>
              <p>尽管本软件的源代码在 GitHub 等平台公开可见，但这绝不意味着本软件遵循任何开源协议。</p>
              <p>源代码公开仅用于学习交流与透明度展示，乙方不因此获得任何开源许可下的复制、修改、分发或再许可权利。</p>
            </div>
          </div>

          <div className="agreement-section">
            <h3>二、著作权与知识产权声明</h3>
            <h4>2.1 软件著作权归属</h4>
            <p>
              本软件（包括但不限于源代码、目标代码、可执行程序、用户界面、交互设计、图标、图形、文案、配置结构、数据库结构及配套文档）的全部著作权、专利权、商标权、商业秘密及其他一切知识产权，均完整、排他地归属于甲方所有。
            </p>
            <p>
              甲方依据本协议授予乙方的，仅为有限的、可撤销的、非独占的、不可再许可的、非商业性的本软件使用许可。乙方所获得的仅是本软件之"使用权"，而非所有权。
            </p>

            <h4>2.2 非开源状态特别声明</h4>
            <p>
              本软件的源代码虽在 GitHub 等平台公开展示，但<strong>本软件不是开源软件</strong>，不遵循、不适用任何开源协议（包括但不限于 MIT、GPL、LGPL、AGPL、Apache、BSD、Mozilla、Unlicense 等）。
            </p>
            <p>
              乙方无权以任何方式复制、修改、改编、翻译、合并、发布、发行、分发、再许可、出售、出租、出借、逆向传播或以其他方式利用本软件之全部或部分源代码、目标代码或衍生作品。
            </p>

            <h4>2.3 商业秘密与技术信息保护</h4>
            <p>
              本软件所包含的算法、架构设计、数据结构、接口设计、性能优化策略、配置解析逻辑及其他未以源代码形式直接体现的技术信息，均构成甲方的商业秘密与未公开技术信息。乙方负有保密义务。
            </p>

            <h4>2.4 反向工程与技术措施禁止</h4>
            <p>除法律另有明确规定外，乙方不得对本软件实施下列行为：</p>
            <p>（一）进行反向工程、反编译、反汇编，或以其他任何方式尝试还原本软件的源代码、内部逻辑或技术原理；</p>
            <p>（二）故意避开、破坏、绕过甲方为保护著作权而采取的技术措施；</p>
            <p>（三）故意删除、篡改、遮挡本软件的权利管理电子信息、版权声明、作者标识、版本标识或其他权属标记；</p>
            <p>（四）对本软件进行二次开发、修改、派生（即"二改"），或将其嵌入、集成、打包至其他软件、产品或服务中。</p>
          </div>

          <div className="agreement-section">
            <h3>三、软件用途与适用地点</h3>
            <h4>3.1 软件用途限定</h4>
            <p>
              本软件的用途严格限定为：供乙方在中华人民共和国境内（特指中国大陆地区，不含香港、澳门及台湾地区）为管理其合法所有或已获合法授权的 ASUS 电脑硬件，进行非商业性的性能调优、监控与配置管理操作。
            </p>
            <p>乙方不得将本软件用于下列用途：</p>
            <p>（一）任何以营利为目的的商业用途，包括但不限于售卖、收费下载、会员制获取、付费订阅、植入广告、捆绑销售；</p>
            <p>（二）对他人设备进行未授权的远程管理、监控、入侵或控制；</p>
            <p>（三）为他人提供本软件的托管、租赁、SaaS 化或类似的商业化服务；</p>
            <p>（四）将本软件用于任何违反中华人民共和国法律法规或公序良俗的用途。</p>

            <h4>3.2 适用地域限制</h4>
            <div className="warning-box">
              <strong>[地域限制] 仅限中国大陆地区使用</strong>
              <p>本软件及其许可、服务、技术支持，仅面向中华人民共和国大陆地区用户提供与使用。</p>
              <p>本协议明确排除香港特别行政区、澳门特别行政区及台湾地区的适用。</p>
            </div>
          </div>

          <div className="agreement-section">
            <h3>四、用户使用限制与禁止行为</h3>
            <h4>4.1 禁止盗版与二改收费</h4>
            <div className="warning-box">
              <strong>[作者声明] 对盗版和二改收费零容忍</strong>
              <p>以下行为均属严重侵权，必将追究到底：</p>
              <p>• 将本软件以任何形式进行售卖、收费下载、会员制获取等牟利行为；</p>
              <p>• 对本软件进行修改、二次开发（二改）后，以任何形式收费或牟利；</p>
              <p>• 将本软件打包进任何收费产品、服务或套餐中；</p>
              <p>• 移除、修改或遮挡本软件的版权声明、作者信息、软件标识；</p>
              <p>• 谎称自己是本软件的作者或开发者，进行任何形式的欺诈；</p>
              <p>• 在港澳台地区及境外分发、传播或提供本软件下载。</p>
            </div>
            <p>甲方对上述行为持零容忍态度。一经发现，将立即采取公开曝光、平台投诉、法律追究等措施。</p>

            <h4>4.2 其他禁止行为</h4>
            <p>（一）未经授权复制、发行、出租、展览、表演、放映、广播、信息网络传播本软件；</p>
            <p>（二）故意避开或者破坏权利人为保护著作权而采取的技术措施；</p>
            <p>（三）故意删除或者改变本软件的权利管理电子信息；</p>
            <p>（四）将本软件转让、出借、出租给第三方使用；</p>
            <p>（五）利用本软件从事任何危害国家安全、社会公共利益或他人合法权益的活动。</p>
          </div>

          <div className="agreement-section">
            <h3>五、网络安全与合规</h3>
            <h4>5.1 遵守网络安全法</h4>
            <p>
              乙方使用本软件应当遵守《中华人民共和国网络安全法》《中华人民共和国数据安全法》《中华人民共和国个人信息保护法》及相关法规，不得利用本软件从事任何危害网络安全的活动。
            </p>

            <h4>5.2 禁止的网络安全行为</h4>
            <p>（一）非法侵入他人计算机信息系统，窃取、非法获取他人数据；</p>
            <p>（二）提供专门用于侵入、非法控制计算机信息系统的程序、工具；</p>
            <p>（三）从事危害网络安全的活动，或为他人从事危害网络安全的活动提供技术支持；</p>
            <p>（四）传播病毒、木马、蠕虫、恶意代码等破坏性程序；</p>
            <p>（五）对他人网站、服务器进行 DDoS 攻击、端口扫描等网络攻击行为；</p>
            <p>（六）窃取、非法买卖、非法提供公民个人信息。</p>
          </div>

          <div className="agreement-section">
            <h3>六、用户数据与隐私保护</h3>
            <h4>6.1 数据收集原则</h4>
            <p>
              本软件尊重并保护用户的个人信息。除法律规定的情形外，未经乙方同意，本软件不会收集、使用、存储、传输或向第三方提供乙方的个人信息。
            </p>

            <h4>6.2 本地数据存储</h4>
            <p>本软件的配置文件、硬件信息等数据默认存储在乙方本地设备上。乙方应当妥善保管自己的数据，定期进行备份。</p>

            <h4>6.3 数据安全责任</h4>
            <p>乙方应对其使用本软件过程中产生的所有数据和操作负责。因乙方自身操作不当、计算机病毒、网络攻击等不可抗力因素导致的数据丢失或泄露，甲方不承担责任。</p>
          </div>

          <div className="agreement-section">
            <h3>七、刑事法律风险提示</h3>
            <h4>7.1 侵犯知识产权犯罪</h4>
            <p>
              以营利为目的，未经著作权人许可，复制发行、通过信息网络向公众传播其文字作品、音乐、美术、视听作品、计算机软件及法律、行政法规规定的其他作品，违法所得数额较大或者有其他严重情节的，将依法追究刑事责任。
            </p>

            <h4>7.2 危害计算机信息系统安全犯罪</h4>
            <p>
              违反国家规定，侵入国家事务、国防建设、尖端科学技术领域的计算机信息系统的；或者违反国家规定，侵入前款规定以外的计算机信息系统或者采用其他技术手段，获取该计算机信息系统中存储、处理或者传输的数据，或者对该计算机信息系统实施非法控制，情节严重的，将依法追究刑事责任。
            </p>

            <h4>7.3 帮助信息网络犯罪活动罪</h4>
            <p>
              明知他人利用信息网络实施犯罪，为其犯罪提供互联网接入、服务器托管、网络存储、通讯传输等技术支持，或者提供广告推广、支付结算等帮助，情节严重的，将依法追究刑事责任。
            </p>
          </div>

          <div className="agreement-section">
            <h3>八、免责声明与责任限制</h3>
            <h4>8.1 现状提供</h4>
            <p>
              本软件按"现状"（AS IS）提供，甲方不对软件的适用性、可靠性、完整性、准确性、不侵权性作出明示或暗示的保证。
            </p>

            <h4>8.2 责任上限</h4>
            <p>
              除因甲方故意或重大过失造成乙方人身损害或法律另有规定外，甲方因本协议或本软件对乙方承担的累计赔偿责任，以乙方实际向甲方支付的对价金额为限。
            </p>
          </div>

          <div className="agreement-section">
            <h3>九、争议解决与法律适用</h3>
            <h4>9.1 法律适用</h4>
            <p>本协议的订立、效力、解释、履行及争议解决，均适用中华人民共和国大陆地区法律。</p>

            <h4>9.2 争议解决</h4>
            <p>
              因本协议引起的或与本协议有关的任何争议，双方应首先友好协商解决；协商不成的，任何一方均有权向甲方住所地有管辖权的人民法院提起诉讼。
            </p>

            <h4>9.3 网络侵权处理</h4>
            <p>
              权利人认为本软件或通过本软件提供的内容侵犯其著作权、商标权等合法权益的，可以向甲方发出权利通知，甲方将依照相关法律法规采取必要措施。
            </p>
          </div>

          <div className="agreement-section">
            <h3>十、未成年人保护</h3>
            <p>
              国家支持研究开发有利于未成年人健康成长的网络产品和服务，依法惩治利用网络从事危害未成年人身心健康的活动。未成年人使用本软件应在监护人的指导和监督下进行。
            </p>
          </div>

          <div className="agreement-section">
            <h3>十一、其他条款</h3>
            <h4>11.1 条款独立性</h4>
            <p>本协议任何条款被有权机关认定为无效或不可执行的，不影响其他条款的效力。</p>

            <h4>11.2 协议修改</h4>
            <p>甲方有权根据需要随时修改本协议内容。修改后的协议将在软件更新时告知乙方。</p>

            <h4>11.3 通知与送达</h4>
            <p>甲方向乙方发送的通知，可以通过软件弹窗、官方网站公告等方式送达。</p>

            <h4>11.4 完整协议</h4>
            <p>本协议构成双方就本软件使用的完整协议，并取代双方此前就同一事项达成的任何口头或书面约定。</p>

            <h4>11.5 协议版本</h4>
            <p>本协议版本：v{currentVersion}</p>
            <p>最后更新日期：2025年</p>
          </div>

          <div className="agreement-section agreement-footer-warning">
            <div className="warning-box warning-final">
              <strong>请您务必注意</strong>
              <p>本软件不是开源软件，源代码公开不等于开源。</p>
              <p>本软件仅限中华人民共和国大陆地区（不含港澳台）使用，仅供非商业性硬件管理用途。</p>
              <p>盗版、二改收费、移除版权信息、违反地域限制等行为必将被追究法律责任，绝不姑息。</p>
            </div>
            <p className="agreement-end">—— 协议内容结束 ——</p>
          </div>
        </div>

        <div className="agreement-footer">
          <span className="agreement-footer-text">
            点击「同意」即表示您已阅读并接受以上全部条款
          </span>
          <div className="agreement-buttons">
            <button className="btn-disagree ripple-container" onClick={(e) => { ripple(e); onDisagree() }}>
              不同意
            </button>
            <button
              className="btn-agree ripple-container"
              onClick={(e) => { ripple(e); onAgree() }}
              disabled={!canAgree}
            >
              {canAgree ? '同意并继续' : `请等待 ${formatTime(countdown)}`}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
