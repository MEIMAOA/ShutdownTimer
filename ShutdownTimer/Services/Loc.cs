using System;
using System.Collections.Generic;

namespace ShutdownTimer.Services
{
    /// <summary>多语言本地化（简体中文 / 繁體中文 / English / 日本語 / 한국어）。</summary>
    public static class Loc
    {
        public const string ZhCN = "zh-CN";
        public const string ZhTW = "zh-TW";
        public const string En = "en";
        public const string Ja = "ja";
        public const string Ko = "ko";

        public static readonly string[] Languages = { ZhCN, ZhTW, En, Ja, Ko };

        public static string Current { get; private set; } = ZhCN;

        public static event Action? LanguageChanged;

        private static readonly Dictionary<string, Dictionary<string, string>> Dict =
            new(StringComparer.Ordinal);

        public static bool IsValid(string? code) => code != null && Array.IndexOf(Languages, code) >= 0;

        public static void Set(string? code)
        {
            if (!IsValid(code)) code = ZhCN;
            if (Current == code) return;
            Current = code!;
            LanguageChanged?.Invoke();
        }

        public static string T(string zh)
        {
            if (Current == ZhCN) return zh;
            if (Dict.TryGetValue(zh, out var map) && map.TryGetValue(Current, out var t))
                return t;

            // 允许首尾空白不一致（XAML 里常用前导空格排版）
            string trimmed = zh.Trim();
            if (trimmed.Length > 0 && trimmed != zh &&
                Dict.TryGetValue(trimmed, out var map2) && map2.TryGetValue(Current, out var t2))
                return t2;

            return zh;
        }

        public static string LanguageName(string? code) => code switch
        {
            ZhTW => "繁體中文",
            En => "English",
            Ja => "日本語",
            Ko => "한국어",
            _ => "简体中文"
        };

        static Loc()
        {
            // ===== 品牌 / 开关 =====
            Add("时序", "時序", "ShutdownTimer", "シャットダウンタイマー", "셧다운 타이머");
            Add("导航", "導覽", "Navigation", "ナビゲーション", "탐색");
            Add("时间选择器", "時間選擇器", "Time picker", "時刻ピッカー", "시간 선택기");
            Add("关", "關", "Off", "オフ", "끔");
            Add("开", "開", "On", "オン", "켬");

            // ===== 导航 =====
            Add("主页", "主頁", "Home", "ホーム", "홈");
            Add("定时关机", "定時關機", "Scheduled Shutdown", "タイマーシャットダウン", "예약 종료");
            Add("启动项", "啟動項", "Startup Items", "スタートアップ項目", "시작 항목");
            Add("应用", "應用", "Apps", "アプリ", "앱");
            Add("系统状态", "系統狀態", "System Status", "システム状態", "시스템 상태");
            Add("清理优化", "清理優化", "Cleanup", "クリーンアップ", "정리 최적화");
            Add("网络连接", "網路連接", "Network", "ネットワーク", "네트워크");
            Add("系统监控", "系統監控", "Monitoring", "システム監視", "시스템 모니터링");
            Add("常用工具", "常用工具", "Tools", "ツール", "도구");
            Add("安全隐私", "安全隱私", "Security & Privacy", "セキュリティとプライバシー", "보안 및 개인 정보");
            Add("自动化", "自動化", "Automation", "自動化", "자동화");
            Add("外观体验", "外觀體驗", "Appearance", "外観", "모양");
            Add("检查更新", "檢查更新", "Check for Updates", "更新チェック", "업데이트 확인");
            Add("软件管控", "軟體管控", "App Control", "アプリ管理", "앱 관리");
            Add("关于", "關於", "About", "情報", "정보");
            Add("设置", "設置", "Settings", "設定", "설정");

            // ===== 通用按钮 =====
            Add("刷新", "重新整理", "Refresh", "更新", "새로 고침");
            Add("删除", "刪除", "Delete", "削除", "삭제");
            Add("移除", "移除", "Remove", "削除", "제거");
            Add("添加", "新增", "Add", "追加", "추가");
            Add("添加时间", "新增時間", "Add Time", "時間を追加", "시간 추가");
            Add("卸载", "解除安裝", "Uninstall", "アンインストール", "제거");
            Add("清空", "清空", "Empty", "空にする", "비우기");
            Add("立即清理", "立即清理", "Clean Now", "今すぐクリーン", "지금 정리");
            Add("立即取消", "立即取消", "Cancel Now", "今すぐキャンセル", "지금 취소");
            Add("开始扫描", "開始掃描", "Start Scan", "スキャン開始", "스캔 시작");
            Add("开始诊断", "開始診斷", "Diagnose", "診断開始", "진단 시작");
            Add("结束进程", "結束處理程序", "End Process", "プロセス終了", "프로세스 종료");
            Add("打开位置", "開啟位置", "Open Location", "場所を開く", "위치 열기");
            Add("访问主页", "造訪首頁", "Visit Homepage", "ホームページへ", "홈페이지 방문");
            Add("查看全文", "檢視全文", "View Full Text", "全文を表示", "전체 보기");
            Add("编辑用户名", "編輯使用者名稱", "Edit Username", "ユーザー名を編集", "사용자 이름 편집");
            Add("更换头像", "更換頭像", "Change Avatar", "アバター変更", "아바타 변경");
            Add("选择", "選擇", "Choose", "選択", "선택");
            Add("默认", "預設", "Default", "既定", "기본");
            Add("重置", "重設", "Reset", "リセット", "초기화");
            Add("退出软件", "退出軟體", "Exit", "終了", "종료");
            Add("显示主窗口", "顯示主視窗", "Show Main Window", "メインウィンドウを表示", "메인 창 표시");
            Add("确定", "確定", "OK", "OK", "확인");
            Add("取消", "取消", "Cancel", "キャンセル", "취소");
            Add("关闭", "關閉", "Close", "閉じる", "닫기");
            Add("保存", "儲存", "Save", "保存", "저장");
            Add("下一步", "下一步", "Next", "次へ", "다음");
            Add("上一步", "上一步", "Back", "戻る", "이전");
            Add("完成", "完成", "Finish", "完了", "완료");
            Add("同意并继续", "同意並繼續", "Agree & Continue", "同意して続行", "동의하고 계속");
            Add("不同意", "不同意", "Disagree", "同意しない", "동의 안 함");
            Add("稍后", "稍後", "Later", "後で", "나중에");
            Add("前往更新", "前往更新", "Go to Update", "更新へ", "업데이트로 이동");
            Add("去登录", "去登入", "Sign In", "サインイン", "로그인");
            Add("卸载失败", "解除安裝失敗", "Uninstall failed", "アンインストール失敗", "제거 실패");
            Add("无法卸载", "無法解除安裝", "Cannot uninstall", "アンインストール不可", "제거할 수 없음");
            Add("卸载应用", "解除安裝應用", "Uninstall App", "アプリをアンインストール", "앱 제거");
            Add("刷新当前页面", "重新整理目前頁面", "Refresh current page", "現在のページを更新", "현재 페이지 새로 고침");
            Add("隐藏到托盘（需开启托盘）", "隱藏到系統匣（需開啟系統匣）", "Hide to tray (tray enabled)", "トレイに隠す（トレイ有効時）", "트레이로 숨기기(트레이 사용 시)");
            Add("切换到对应页面", "切換到對應頁面", "Switch to page", "ページへ切り替え", "해당 페이지로 전환");
            Add("打开网络设置", "開啟網路設定", "Open Network Settings", "ネットワーク設定を開く", "네트워크 설정 열기");
            Add("打开防火墙", "開啟防火牆", "Open Firewall", "ファイアウォールを開く", "방화벽 열기");
            Add("摄像头", "攝影機", "Camera", "カメラ", "카메라");
            Add("麦克风", "麥克風", "Microphone", "マイク", "마이크");
            Add("位置", "位置", "Location", "位置情報", "위치");
            Add("全部隐私设置", "全部隱私設定", "All Privacy Settings", "すべてのプライバシー設定", "모든 개인 정보 설정");

            // ===== 主页 =====
            Add("来自 GitHub 的最新资讯", "來自 GitHub 的最新資訊", "Latest News from GitHub", "GitHub からの最新情報", "GitHub 최신 소식");
            Add("正在加载…", "正在載入…", "Loading…", "読み込み中…", "로드 중…");
            Add("加载失败", "載入失敗", "Load failed", "読み込み失敗", "로드 실패");
            Add("接口限流", "介面限流", "API rate limit", "API 制限", "API 제한");
            Add("共", "共", "Total", "合計", "총");
            Add("个仓库", "個倉庫", "repos", "リポジトリ", "개 저장소");
            Add("总计", "總計", "stars", "スター", "별");
            Add("更新于", "更新於", "updated", "更新", "업데이트");
            Add("缓存于", "快取於", "cached", "キャッシュ", "캐시");
            Add("（缓存）", "（快取）", " (cached)", "（キャッシュ）", " (캐시)");
            Add("未能获取到资讯（网络不可用或请求超时）。点击「刷新」重试。",
                "未能取得資訊（網路不可用或請求逾時）。點擊「重新整理」重試。",
                "Could not fetch news (network unavailable or timeout). Click Refresh to retry.",
                "情報を取得できませんでした。「更新」で再試行してください。",
                "소식을 가져오지 못했습니다. 새로 고침을 눌러 다시 시도하세요.");
            Add("GitHub 接口触发限流（未登录每小时约 60 次），请稍后再试；点击「刷新」重新拉取。",
                "GitHub 介面觸發限流（未登入每小時約 60 次），請稍後再試；點擊「重新整理」重新拉取。",
                "GitHub API rate limit reached (~60/hour unauthenticated). Retry later or click Refresh.",
                "GitHub API の制限に達しました(未ログインで毎時約60回)。後でもう一度お試しください。",
                "GitHub API 제한에 도달했습니다(시간당 약 60회). 나중에 다시 시도하세요.");
            Add("当前为本地缓存数据（GitHub 接口可能被限流），可点击「刷新」尝试重新拉取。",
                "當前為本地快取資料（GitHub 介面可能被限流），可點擊「重新整理」嘗試重新拉取。",
                "Showing cached data (GitHub API may be rate-limited). Click Refresh to retry.",
                "ローカルキャッシュを表示しています。「更新」で再取得を試みてください。",
                "캐시된 데이터를 표시 중입니다. 새로 고침으로 다시 시도하세요.");

            // ===== 设置 =====
            Add("默认启动页", "預設啟動頁", "Default Startup Page", "既定の起動ページ", "기본 시작 페이지");
            Add("每次打开程序时默认进入的页面", "每次開啟程式時預設進入的頁面", "Page opened by default on launch", "起動時に既定で開くページ", "앱 시작 시 기본으로 열리는 페이지");
            Add("语言", "語言", "Language", "言語", "언어");
            Add("选择界面显示语言", "選擇介面顯示語言", "Choose display language", "表示言語を選択", "표시 언어 선택");
            Add("在系统托盘中显示", "在系統托盤中顯示", "Show in system tray", "システムトレイに表示", "시스템 트레이에 표시");
            Add("关闭窗口时继续在后台运行", "關閉視窗時繼續在背景執行", "Keep running in background when closed", "ウィンドウを閉じてもバックグラウンドで実行", "창을 닫아도 백그라운드에서 계속 실행");
            Add("调整用户信息、窗口外观与系统集成选项", "調整使用者資訊、視窗外觀與系統整合選項", "Adjust user info, appearance and system integration", "ユーザー情報・外観・システム連携を調整", "사용자 정보, 창 모양, 시스템 통합 옵션 조정");
            Add("用户名", "使用者名稱", "Username", "ユーザー名", "사용자 이름");
            Add("此项已被锁定，无法修改", "此項目已被鎖定，無法修改", "This item is locked and cannot be modified", "この項目はロックされており変更できません", "이 항목은 잠겨 있어 수정할 수 없습니다");
            Add("外观", "外觀", "Appearance", "外観", "모양");
            Add("窗口透明度", "視窗透明度", "Window Opacity", "ウィンドウ透過度", "창 투명도");
            Add("值越大越透明（需关闭背景图）", "值越大越透明（需關閉背景圖）", "Higher = more transparent (background off)", "大きいほど透明（背景画像オフ時）", "클수록 투명(배경 이미지 꺼짐)");
            Add("显示背景图片", "顯示背景圖片", "Show Background Image", "背景画像を表示", "배경 이미지 표시");
            Add("关闭后窗口呈透明效果", "關閉後視窗呈透明效果", "Window becomes transparent when off", "オフでウィンドウが透明に", "끄면 창이 투명해집니다");
            Add("背景图片", "背景圖片", "Background Image", "背景画像", "배경 이미지");
            Add("帮助", "說明", "Help", "ヘルプ", "도움말");
            Add("重看所有教程", "重看所有教學", "Replay All Tutorials", "すべてのチュートリアルを再表示", "모든 튜토리얼 다시 보기");
            Add("下次进入每个页面时重新显示使用教程", "下次進入每個頁面時重新顯示使用教學", "Show tutorials again on next visit", "次回各ページでチュートリアルを再表示", "다음 방문 시 튜토리얼 다시 표시");
            Add("系统集成", "系統整合", "System Integration", "システム連携", "시스템 통합");
            Add("危险选项", "危險選項", "Danger Zone", "危険な設定", "위험 옵션");
            Add("功能锁定", "功能鎖定", "Feature Lock", "機能ロック", "기능 잠금");
            Add("锁定定时关机", "鎖定定時關機", "Lock Scheduled Shutdown", "シャットダウンをロック", "예약 종료 잠금");
            Add("锁定托盘显示", "鎖定托盤顯示", "Lock Tray Icon", "トレイ表示をロック", "트레이 표시 잠금");
            Add("锁定用户名和头像", "鎖定使用者名稱和頭像", "Lock Username & Avatar", "ユーザー名とアバターをロック", "사용자 이름·아바타 잠금");
            Add("锁定窗口透明度", "鎖定視窗透明度", "Lock Window Opacity", "透過度をロック", "창 투명도 잠금");
            Add("锁定背景图", "鎖定背景圖", "Lock Background Image", "背景画像をロック", "배경 이미지 잠금");
            Add("锁定教程重置", "鎖定教學重設", "Lock Tutorial Reset", "チュートリアルリセットをロック", "튜토리얼 초기화 잠금");
            Add("锁定仅在本程序界面内有效，用户仍可通过手动编辑 config.json 修改设置。",
                "鎖定僅在本程式介面內有效，使用者仍可透過手動編輯 config.json 修改設定。",
                "Locks only apply within this app; users can still edit config.json manually.",
                "ロックはこのアプリ内でのみ有効です。config.json を手動編集すれば変更可能です。",
                "잠금은 이 앱 내에서만 유효하며 config.json을 수동 편집해 변경할 수 있습니다.");
            Add("请谨慎使用", "請謹慎使用", "Use with caution", "注意して使用してください", "주의해서 사용하세요");

            // ===== 定时关机页 =====
            Add("所有更改都会立即保存并生效", "所有變更都會立即儲存並生效", "All changes are saved and applied immediately", "変更はすぐに保存・適用されます", "모든 변경 사항은 즉시 저장·적용됩니다");
            Add("常规", "一般", "General", "一般", "일반");
            Add("启用时序", "啟用時序", "Enable Schedule", "スケジュールを有効化", "예약 활성화");
            Add("开启后将在每天指定时间自动关闭计算机", "開啟後將在每天指定時間自動關閉電腦", "Automatically shut down at scheduled times", "指定時刻に自動シャットダウン", "지정 시간에 자동으로 컴퓨터 종료");
            Add("关机时间", "關機時間", "Shutdown Time", "シャットダウン時刻", "종료 시간");
            Add("可添加多个时间段，并指定每天或某一天关机", "可新增多個時間段，並指定每天或某一天關機", "Add multiple times, daily or specific day", "毎日または特定曜日の時刻を追加", "여러 시간과 요일별 종료 추가");
            Add("开机自动启动", "開機自動啟動", "Start with Windows", "Windows 起動時に自動実行", "Windows 시작 시 자동 실행");
            Add("登录 Windows 后自动在后台静默运行", "登入 Windows 後自動在背景靜默執行", "Run silently in background after sign-in", "サインイン後にバックグラウンドで実行", "로그인 후 백그라운드에서 자동 실행");
            Add("操作", "操作", "Actions", "操作", "동작");
            Add("取消关机", "取消關機", "Cancel Shutdown", "シャットダウンをキャンセル", "종료 취소");
            Add("如果系统正在倒计时关机，可立即取消", "如果系統正在倒數關機，可立即取消", "Cancel the pending shutdown immediately", "保留中のシャットダウンをキャンセル", "진행 중인 종료를 즉시 취소");
            Add("每天", "每天", "Every day", "毎日", "매일");
            Add("周日", "週日", "Sun", "日", "일");
            Add("周一", "週一", "Mon", "月", "월");
            Add("周二", "週二", "Tue", "火", "화");
            Add("周三", "週三", "Wed", "水", "수");
            Add("周四", "週四", "Thu", "木", "목");
            Add("周五", "週五", "Fri", "金", "금");
            Add("周六", "週六", "Sat", "土", "토");

            // ===== 自动化 =====
            Add("让电脑按计划自动执行任务", "讓電腦按計畫自動執行任務", "Run tasks automatically on schedule", "スケジュールに沿って自動実行", "일정에 따라 자동으로 작업 실행");
            Add("定时重启", "定時重新啟動", "Scheduled Restart", "タイマー再起動", "예약 재시작");
            Add("每天自动重启", "每天自動重新啟動", "Auto restart every day", "毎日自動再起動", "매일 자동 재시작");
            Add("到达指定时间后自动重启电脑", "到達指定時間後自動重新啟動電腦", "Restart at the specified time", "指定時刻に再起動", "지정 시간에 자동 재시작");
            Add("定时休眠", "定時休眠", "Scheduled Hibernate", "タイマー休止", "예약 최대 절전");
            Add("每天自动休眠", "每天自動休眠", "Auto hibernate every day", "毎日自動休止", "매일 자동 최대 절전");
            Add("到达指定时间后自动进入休眠", "到達指定時間後自動進入休眠", "Hibernate at the specified time", "指定時刻に休止", "지정 시간에 자동 최대 절전");
            Add("定时锁定", "定時鎖定", "Auto Lock", "自動ロック", "자동 잠금");
            Add("无操作后自动锁定", "無操作後自動鎖定", "Auto lock after inactivity", "無操作で自動ロック", "미사용 시 자동 잠금");
            Add("设为 0 分钟则关闭此功能", "設為 0 分鐘則關閉此功能", "Set to 0 to disable", "0 分で無効", "0분으로 설정하면 기능 끔");
            Add("立即测试：锁定屏幕", "立即測試：鎖定螢幕", "Test Now: Lock Screen", "今すぐテスト：画面ロック", "지금 테스트: 화면 잠금");
            Add("立即测试：休眠", "立即測試：休眠", "Test Now: Hibernate", "今すぐテスト：休止", "지금 테스트: 최대 절전");
            Add("取消已计划的关机/重启", "取消已計畫的關機/重新啟動", "Cancel scheduled shutdown/restart", "予定のシャットダウン/再起動をキャンセル", "예약된 종료/재시작 취소");

            // ===== 应用 / 启动项 / 监控 =====
            Add("应用管理", "應用管理", "App Management", "アプリ管理", "앱 관리");
            Add("查看和卸载系统上已安装的应用", "檢視和解除安裝系統上已安裝的應用", "View and uninstall installed apps", "インストール済みアプリの表示と削除", "설치된 앱 보기 및 제거");
            Add("搜索应用名称…", "搜尋應用名稱…", "Search apps…", "アプリ名を検索…", "앱 검색…");
            Add("启动项管理", "啟動項管理", "Startup Management", "スタートアップ管理", "시작 항목 관리");
            Add("管理系统开机时自动运行的程序", "管理系統開機時自動執行的程式", "Manage programs that start with Windows", "起動時に実行するプログラムを管理", "Windows 시작 시 실행되는 프로그램 관리");
            Add("搜索进程…", "搜尋處理程序…", "Search processes…", "プロセスを検索…", "프로세스 검색…");
            Add("加载中…", "載入中…", "Loading…", "読み込み中…", "로드 중…");

            // ===== 清理优化 =====
            Add("释放磁盘空间，让电脑更流畅", "釋放磁碟空間，讓電腦更流暢", "Free up disk space for a smoother PC", "ディスクを解放して快適に", "디스크 공간 확보로 더 빠르게");
            Add("一键清理", "一鍵清理", "One-Click Cleanup", "ワンクリッククリーン", "원클릭 정리");
            Add("临时文件清理", "暫存檔清理", "Temporary File Cleanup", "一時ファイルの削除", "임시 파일 정리");
            Add("清空回收站", "清空資源回收筒", "Empty Recycle Bin", "ごみ箱を空にする", "휴지통 비우기");
            Add("删除回收站里的所有文件", "刪除資源回收筒裡的所有檔案", "Delete all files in the recycle bin", "ごみ箱内の全ファイルを削除", "휴지통의 모든 파일 삭제");
            Add("大文件扫描", "大檔案掃描", "Large File Scan", "大容量ファイルスキャン", "대용량 파일 스캔");
            Add("扫描目录", "掃描目錄", "Scan Directory", "スキャンディレクトリ", "스캔 디렉터리");
            Add("大于 100 MB", "大於 100 MB", "> 100 MB", "100 MB 以上", "100MB 초과");
            Add("大于 500 MB", "大於 500 MB", "> 500 MB", "500 MB 以上", "500MB 초과");
            Add("大于 1 GB", "大於 1 GB", "> 1 GB", "1 GB 以上", "1GB 초과");
            Add("大于 5 GB", "大於 5 GB", "> 5 GB", "5 GB 以上", "5GB 초과");
            Add("计算中…", "計算中…", "Calculating…", "計算中…", "계산 중…");
            Add("扫描中…", "掃描中…", "Scanning…", "スキャン中…", "스캔 중…");
            Add("清理临时文件", "清理暫存檔", "Clean Temporary Files", "一時ファイルを削除", "임시 파일 정리");
            Add("清理完成", "清理完成", "Cleanup Complete", "クリーンアップ完了", "정리 완료");
            Add("清理", "清理", "Clean", "クリーン", "정리");
            Add("将删除 %TEMP% 和 Windows\\Temp 中的临时文件，确定继续？",
                "將刪除 %TEMP% 和 Windows\\Temp 中的暫存檔，確定繼續？",
                "Delete temporary files in %TEMP% and Windows\\Temp. Continue?",
                "%TEMP% と Windows\\Temp の一時ファイルを削除します。続行しますか？",
                "%TEMP% 및 Windows\\Temp의 임시 파일을 삭제합니다. 계속할까요?");
            Add("确定要清空回收站吗？此操作不可撤销。",
                "確定要清空資源回收筒嗎？此操作無法復原。",
                "Empty the recycle bin? This cannot be undone.",
                "ごみ箱を空にしますか？この操作は元に戻せません。",
                "휴지통을 비울까요? 이 작업은 되돌릴 수 없습니다.");
            Add("删除了", "刪除了", "Deleted", "削除:", "삭제됨");
            Add("个文件，释放", "個檔案，釋放", "file(s), freed", "ファイル、解放", "개 파일, 확보");

            // ===== 网络 =====
            Add("检查网络状态、诊断连接问题", "檢查網路狀態、診斷連線問題", "Check network status and diagnose issues", "ネットワーク状態の確認と診断", "네트워크 상태 확인 및 문제 진단");
            Add("网络诊断", "網路診斷", "Network Diagnostics", "ネットワーク診断", "네트워크 진단");
            Add("网络适配器", "網路介面卡", "Network Adapters", "ネットワークアダプター", "네트워크 어댑터");

            // ===== 安全 =====
            Add("检查 Defender 状态，快速打开隐私设置", "檢查 Defender 狀態，快速開啟隱私設定", "Check Defender status and open privacy settings", "Defender の状態確認とプライバシー設定", "Defender 상태 확인 및 개인 정보 설정");
            Add("实时保护", "即時保護", "Real-time Protection", "リアルタイム保護", "실시간 보호");
            Add("病毒库更新时间", "病毒庫更新時間", "Virus Definitions Updated", "ウイルス定義更新日時", "바이러스 정의 업데이트");
            Add("隐私设置快捷入口", "隱私設定快捷入口", "Privacy Shortcuts", "プライバシー設定ショートカット", "개인 정보 설정 바로가기");

            // ===== 软件管控（教育版）=====
            Add("教育分支版专属：禁止打开或限制软件的使用时长与使用时间段",
                "教育分支版專屬：禁止開啟或限制軟體的使用時長與使用時間段",
                "Education only: block or limit app usage time and windows",
                "教育版限定：アプリの使用時間・時間帯を制限",
                "교육 전용: 앱 사용 시간 및 시간대 제한");
            Add("添加管控规则", "新增管控規則", "Add Control Rule", "ルールを追加", "제어 규칙 추가");
            Add("进程名或完整路径，如 game 或 C:\\path\\game.exe",
                "處理程序名稱或完整路徑，如 game 或 C:\\path\\game.exe",
                "Process name or full path, e.g. game",
                "プロセス名またはフルパス（例: game）",
                "프로세스 이름 또는 전체 경로(예: game)");
            Add("完全禁止", "完全禁止", "Fully Block", "完全にブロック", "완전 차단");
            Add("每日限时", "每日限時", "Daily Limit", "毎日の時間制限", "매일 시간 제한");
            Add("不限时长", "不限時長", "No Time Limit", "時間制限なし", "시간 제한 없음");
            Add("每日最多使用（分钟）：", "每日最多使用（分鐘）：", "Max minutes per day:", "1日の最大使用時間(分):", "하루 최대 사용(분):");
            Add("限制使用时间段", "限制使用時間段", "Restrict Time Window", "使用時間帯を制限", "사용 시간대 제한");
            Add("不限制", "不限制", "No restriction", "制限なし", "제한 없음");
            Add("仅允许在以下时间段使用", "僅允許在以下時間段使用", "Allow only during this window", "この時間帯のみ許可", "이 시간대에만 허용");
            Add("尚未添加任何规则", "尚未新增任何規則", "No rules added yet", "ルールはまだありません", "아직 규칙이 없습니다");

            // ===== 更新页 =====
            Add("从 GitHub Releases 获取最新版本", "從 GitHub Releases 取得最新版本", "Get the latest version from GitHub Releases", "GitHub Releases から最新版を取得", "GitHub Releases에서 최신 버전 가져오기");
            Add("版本信息", "版本資訊", "Version Info", "バージョン情報", "버전 정보");
            Add("当前版本", "目前版本", "Current Version", "現在のバージョン", "현재 버전");
            Add("更新渠道", "更新管道", "Update Channel", "更新チャネル", "업데이트 채널");
            Add("选择从哪个渠道接收更新", "選擇從哪個管道接收更新", "Choose which channel to update from", "更新元チャネルを選択", "업데이트를 받을 채널 선택");
            Add("正式版", "正式版", "Stable", "安定版", "정식 버전");
            Add("教育分支版", "教育分支版", "Education", "教育版", "교육 버전");
            Add("下载并安装更新", "下載並安裝更新", "Download & Install", "ダウンロードしてインストール", "다운로드 및 설치");
            Add("更新", "更新", "Update", "更新", "업데이트");
            Add("点击上方「检查更新」按钮开始检测。", "點擊上方「檢查更新」按鈕開始檢測。", "Click Check for Updates above to start.", "上の「更新チェック」をクリックして開始。", "위의 업데이트 확인 버튼을 눌러 시작하세요.");
            Add("关于自动更新", "關於自動更新", "About Auto Update", "自動更新について", "자동 업데이트 정보");
            Add("已是最新版本", "已是最新版本", "You're up to date", "最新バージョンです", "최신 버전입니다");
            Add("发现新版本", "發現新版本", "New Version Found", "新しいバージョン", "새 버전 발견");
            Add("将切换到", "將切換到", "Switch to", "切り替え:", "전환:");

            // ===== 关于页 =====
            Add("时序程序的相关信息", "時序程式的相關資訊", "About this program", "このプログラムについて", "이 프로그램 정보");
            Add("应用信息", "應用資訊", "App Info", "アプリ情報", "앱 정보");
            Add("应用名称", "應用名稱", "App Name", "アプリ名", "앱 이름");
            Add("版本", "版本", "Version", "バージョン", "버전");
            Add("版权信息", "版權資訊", "Copyright", "著作権", "저작권");
            Add("© 2026 MEIMAOA 版权所有", "© 2026 MEIMAOA 版權所有", "© 2026 MEIMAOA All rights reserved", "© 2026 MEIMAOA All rights reserved", "© 2026 MEIMAOA All rights reserved");
            Add("开源许可", "開源授權", "Open Source License", "オープンソースライセンス", "오픈 소스 라이선스");
            Add("本软件依据 Apache 2.0 许可证授权", "本軟體依據 Apache 2.0 授權條款授權", "Licensed under Apache 2.0", "Apache 2.0 ライセンス", "Apache 2.0 라이선스");
            Add("用户条款", "使用者條款", "Terms", "利用規約", "약관");
            Add("用户使用条款", "使用者使用條款", "Terms of Service", "利用規約", "이용 약관");
            Add("查看本软件的使用条款和免责声明", "檢視本軟體的使用條款和免責聲明", "View terms and disclaimers", "利用規約と免責事項を表示", "이용 약관 및 면책 조항 보기");
            Add("存储位置", "儲存位置", "Storage", "保存場所", "저장 위치");
            Add("配置文件", "設定檔", "Config File", "設定ファイル", "구성 파일");
            Add("退出", "退出", "Exit", "終了", "종료");
            Add("完全退出时序（关闭定时任务与软件管控）", "完全退出時序（關閉定時任務與軟體管控）", "Fully exit (stop schedules and app control)", "完全に終了（スケジュールとアプリ管理を停止）", "완전히 종료(예약 및 앱 관리 중지)");

            // ===== 条款 / 引导 =====
            Add("请在继续使用前仔细阅读以下条款", "請在繼續使用前仔細閱讀以下條款", "Please read the following terms carefully", "続行前に以下の規約をよくお読みください", "계속하기 전에 아래 약관을 읽어 주세요");
            Add("点击「同意」表示您接受本条款的全部内容", "點擊「同意」表示您接受本條款的全部內容", "Clicking Agree means you accept all terms", "「同意」をクリックすると全条項に同意したことになります", "동의를 누르면 모든 약관에 동의함을 의미합니다");
            Add("一个帮你管理电脑、让作息更规律的小助手。", "一個幫你管理電腦、讓作息更規律的小助手。", "A helper that manages your PC and keeps a regular routine.", "PC 管理と規則的な生活を助けるアシスタント。", "PC 관리와 규칙적인 생활을 돕는 도우미.");
            Add("接下来只需 4 步就能完成初始化配置。", "接下來只需 4 步就能完成初始化設定。", "Just 4 steps to complete setup.", "あと 4 ステップで初期設定が完了します。", "4단계만 거치면 초기 설정이 완료됩니다.");
            Add("每个步骤都有详细说明，点击「下一步」继续。", "每個步驟都有詳細說明，點擊「下一步」繼續。", "Each step has instructions; click Next.", "各ステップの説明を読み「次へ」で進みます。", "각 단계의 설명을 보고 다음을 눌러 진행하세요.");
            Add("选择配置方案", "選擇設定方案", "Choose a Setup Plan", "設定プランを選択", "설정 플랜 선택");
            Add("根据你的使用习惯选择，稍后随时可以修改。", "根據你的使用習慣選擇，稍後隨時可以修改。", "Choose based on your habits; change anytime.", "使用習慣に合わせて選択（後から変更可）。", "사용 습관에 따라 선택하세요(나중에 변경 가능).");
            Add("⭐ 推荐配置", "⭐ 推薦設定", "⭐ Recommended", "⭐ おすすめ", "⭐ 권장 설정");
            Add("⚙️ 自定义配置", "⚙️ 自訂設定", "⚙️ Custom", "⚙️ カスタム", "⚙️ 사용자 지정");
            Add("🍃 极简配置", "🍃 極簡設定", "🍃 Minimal", "🍃 最小限", "🍃 최소 설정");
            Add("个性化", "個人化", "Personalization", "パーソナライズ", "개인 설정");
            Add("让程序更符合你的风格。", "讓程式更符合你的風格。", "Make the app feel like yours.", "自分好みにカスタマイズ。", "앱을 내 스타일로.");
            Add("你的称呼（可选）", "你的稱呼（可選）", "Your Name (optional)", "名前（任意）", "이름(선택)");
            Add("输入你的名字", "輸入你的名字", "Enter your name", "名前を入力", "이름 입력");
            Add("界面主题", "介面主題", "Theme", "テーマ", "테마");
            Add("跟随系统", "跟隨系統", "System Default", "システムに従う", "시스템 기본");
            Add("浅色", "淺色", "Light", "ライト", "라이트");
            Add("深色", "深色", "Dark", "ダーク", "다크");
            Add("🎉 全部就绪！", "🎉 全部就緒！", "🎉 All Set!", "🎉 準備完了！", "🎉 모두 준비 완료!");
            Add("点击「完成」启动程序。", "點擊「完成」啟動程式。", "Click Finish to launch.", "「完了」をクリックして起動。", "완료를 눌러 시작하세요.");
            Add("点击空白处关闭", "點擊空白處關閉", "Click anywhere to close", "空白をクリックして閉じる", "빈 곳을 클릭하여 닫기");
            Add("欢迎 - 初始化配置", "歡迎 - 初始化設定", "Welcome - Setup", "ようこそ - 初期設定", "환영 - 초기 설정");

            // ===== 系统状态 / 工具 =====
            Add("系统设置", "系統設定", "System Settings", "システム設定", "시스템 설정");
            Add("管理工具", "管理工具", "Admin Tools", "管理ツール", "관리 도구");
            Add("常用程序", "常用程式", "Common Programs", "よく使うプログラム", "자주 쓰는 프로그램");

            // ===== 弹窗 / 提示 =====
            Add("建议登录微软账户", "建議登入微軟帳戶", "Sign in to a Microsoft account", "Microsoft アカウントへのサインイン推奨", "Microsoft 계정 로그인 권장");
            Add("登录后可跨设备同步设置、更方便找回密码，还能使用 OneDrive。", "登入後可跨裝置同步設定、更方便找回密碼，還能使用 OneDrive。", "Sync settings, recover passwords, and use OneDrive.", "設定の同期・パスワード回復・OneDrive が利用できます。", "설정 동기화, 비밀번호 복구, OneDrive 사용 가능.");
            Add("检测到你当前使用的是本地账户。\n\n登录微软账户可以：\n• 跨设备同步设置和文件\n• 更方便地找回密码\n• 使用 OneDrive 云存储\n\n是否现在去系统设置？",
                "偵測到你目前使用的是本機帳戶。\n\n登入微軟帳戶可以：\n• 跨裝置同步設定和檔案\n• 更方便地找回密碼\n• 使用 OneDrive 雲端儲存\n\n是否現在去系統設定？",
                "You're using a local account.\n\nSign in to a Microsoft account to:\n• Sync settings and files across devices\n• Recover your password more easily\n• Use OneDrive cloud storage\n\nOpen system settings now?",
                "現在はローカルアカウントを使用しています。\n\nMicrosoft アカウントでサインインすると：\n• 設定やファイルをデバイス間で同期\n• パスワードを簡単に回復\n• OneDrive クラウドストレージを使用\n\n今すぐシステム設定を開きますか？",
                "현재 로컬 계정을 사용 중입니다.\n\nMicrosoft 계정으로 로그인하면:\n• 기기 간 설정·파일 동기화\n• 비밀번호를 쉽게 복구\n• OneDrive 클라우드 저장소 사용\n\n지금 시스템 설정을 열까요?");
            Add("是否前往「检查更新」页面？", "是否前往「檢查更新」頁面？", "Go to the update page?", "更新ページへ移動しますか？", "업데이트 페이지로 이동할까요?");
            Add("更新内容：", "更新內容：", "What's new:", "更新内容:", "업데이트 내용:");
            Add("当前版本：", "目前版本：", "Current version:", "現在のバージョン:", "현재 버전:");
            Add("最新版本：", "最新版本：", "Latest version:", "最新バージョン:", "최신 버전:");
            Add("删除启动项", "刪除啟動項", "Delete Startup Item", "スタートアップ項目を削除", "시작 항목 삭제");
            Add("结束", "結束", "End", "終了", "종료");
            Add("无法结束进程", "無法結束處理程序", "Cannot End Process", "プロセスを終了できません", "프로세스를 종료할 수 없음");
            Add("请输入新的用户名（1-20 字）", "請輸入新的使用者名稱（1-20 字）", "Enter a new username (1-20 chars)", "新しいユーザー名を入力（1-20文字）", "새 사용자 이름 입력(1-20자)");
            Add("用户名不能为空", "使用者名稱不能為空", "Username cannot be empty", "ユーザー名を空にはできません", "사용자 이름은 비워둘 수 없습니다");
            Add("请重新输入用户名。", "請重新輸入使用者名稱。", "Please re-enter the username.", "ユーザー名を再入力してください。", "사용자 이름을 다시 입력하세요.");
            Add("重看教程", "重看教學", "Replay Tutorials", "チュートリアルを再表示", "튜토리얼 다시 보기");
            Add("重置后，下次进入每个页面时会重新显示使用教程。", "重設後，下次進入每個頁面時會重新顯示使用教學。", "After reset, tutorials will show again on next visit.", "リセット後、次回各ページでチュートリアルが再表示されます。", "초기화 후 다음 방문 시 튜토리얼이 다시 표시됩니다.");
            Add("定时关机程序已在运行中。\n\n是否打开已开启的程序窗口？",
                "定時關機程式已在執行中。\n\n是否開啟已開啟的程式視窗？",
                "The program is already running.\n\nOpen the existing window?",
                "プログラムは既に実行中です。\n\n既存のウィンドウを開きますか？",
                "프로그램이 이미 실행 중입니다.\n\n기존 창을 열까요?");

            // ===== 补充：工具 / 硬件 / 更新说明 / 锁定说明 / 引导 =====
            Add("• 更新包从 GitHub Releases 下载，格式为 zip",
                "• 更新包從 GitHub Releases 下載，格式為 zip",
                "• Update packages are downloaded from GitHub Releases as zip",
                "• 更新パッケージは GitHub Releases から zip 形式でダウンロード",
                "• 업데이트 패키지는 GitHub Releases에서 zip 형식으로 다운로드");
            Add("• 下载完成后程序会自动退出，重启后即为新版本",
                "• 下載完成後程式會自動結束，重新啟動即為新版本",
                "• After download the app exits automatically; restart to apply",
                "• ダウンロード後アプリは自動終了します。再起動で新バージョンになります",
                "• 다운로드 후 앱이 자동 종료됩니다. 재시작하면 새 버전이 적용됩니다");
            Add("• 更新期间请勿手动关闭程序，否则可能导致更新失败",
                "• 更新期間請勿手動關閉程式，否則可能導致更新失敗",
                "• Do not close the app during updates, or the update may fail",
                "• 更新中はアプリを閉じないでください（失敗の原因になります）",
                "• 업데이트 중에는 앱을 닫지 마세요(실패할 수 있습니다)");
            Add("• 如自动更新失败，可手动下载 zip 解压覆盖",
                "• 如自動更新失敗，可手動下載 zip 解壓縮覆蓋",
                "• If auto update fails, download the zip and overwrite manually",
                "• 自動更新に失敗した場合、zip を手動でダウンロードして上書きしてください",
                "• 자동 업데이트 실패 시 zip을 수동으로 내려받아 덮어쓰세요");
            Add("👋 欢迎使用时序", "👋 歡迎使用時序", "👋 Welcome to ShutdownTimer", "👋 シャットダウンタイマーへようこそ", "👋 셧다운 타이머에 오신 것을 환영합니다");
            Add("一键打开 Windows 里藏得比较深的工具",
                "一鍵開啟 Windows 裡藏得比較深的工具",
                "One-click access to Windows tools buried deep",
                "Windows の奥深くにあるツールをワンクリックで",
                "Windows의 숨은 도구를 원클릭으로");
            Add("不开启任何自动功能，只保留基础界面。",
                "不開啟任何自動功能，只保留基礎介面。",
                "No automatic features; keep only the basic interface.",
                "自動機能は有効にせず、基本インターフェースのみ。",
                "자동 기능 없이 기본 인터페이스만 유지.");
            Add("保持默认值，稍后由你自己在设置里调整各项功能。",
                "保持預設值，稍後由你自己在設定裡調整各項功能。",
                "Keep defaults; adjust features later in Settings.",
                "既定値のままにして、後で設定で調整します。",
                "기본값을 유지하고 나중에 설정에서 조정하세요.");
            Add("主题", "主題", "Theme", "テーマ", "테마");
            Add("快捷键", "快速鍵", "Shortcuts", "ショートカット", "단축키");
            Add("任务管理器", "工作管理員", "Task Manager", "タスクマネージャー", "작업 관리자");
            Add("你的称呼", "你的稱呼", "Your Name", "あなたの名前", "이름");
            Add("内存", "記憶體", "Memory", "メモリ", "메모리");
            Add("启动应用", "啟動應用", "Startup Apps", "スタートアップアプリ", "시작 앱");
            Add("命令提示符", "命令提示字元", "Command Prompt", "コマンドプロンプト", "명령 프롬프트");
            Add("处理器", "處理器", "Processor", "プロセッサ", "프로세서");
            Add("头像", "頭像", "Avatar", "アバター", "아바타");
            Add("存储", "儲存", "Storage", "ストレージ", "저장소");
            Add("开机时长", "開機時長", "Uptime", "稼働時間", "가동 시간");
            Add("必填，例如：小明", "必填，例如：小明", "Required, e.g. Alex", "必須（例：たろう）", "필수, 예: 철수");
            Add("控制面板", "控制台", "Control Panel", "コントロールパネル", "제어판");
            Add("操作系统", "作業系統", "OS", "OS", "OS");
            Add("教育分支版（含软件管控功能）", "教育分支版（含軟體管控功能）", "Education (with app control)", "教育版（アプリ管理機能付き）", "교육 버전(앱 관리 기능 포함)");
            Add("文件资源管理器", "檔案總管", "File Explorer", "エクスプローラー", "파일 탐색기");
            Add("时序用户", "時序使用者", "ShutdownTimer User", "シャットダウンタイマーユーザー", "셧다운 타이머 사용자");
            Add("显示", "顯示", "Display", "ディスプレイ", "디스플레이");
            Add("服务", "服務", "Services", "サービス", "서비스");
            Add("查看电脑硬件和系统的整体情况", "檢視電腦硬體和系統的整體情況", "View overall hardware and system info", "ハードウェアとシステムの全体像を表示", "하드웨어와 시스템 전반 정보 보기");
            Add("核心数", "核心數", "Cores", "コア数", "코어 수");
            Add("每个页面首次进入时都会显示使用教程，逐步教你操作。", "每個頁面首次進入時都會顯示使用教學，逐步教你操作。", "Each page shows a tutorial on first visit, step by step.", "各ページ初回表示時にチュートリアルが表示され、順を追って案内します。", "각 페이지 첫 방문 시 튜토리얼이 표시되어 단계별로 안내합니다.");
            Add("每晚 23:00 自动关机，开启开机自启、系统托盘。适合大多数用户。", "每晚 23:00 自動關機，開啟開機自啟、系統匣。適合大多數使用者。", "Auto shutdown at 23:00, enable startup and tray. Suitable for most users.", "毎晩 23:00 に自動シャットダウン。自動起動とトレイを有効にします。ほとんどのユーザーに最適。", "매일 밤 23:00 자동 종료, 자동 시작 및 트레이 사용. 대부분의 사용자에게 적합.");
            Add("每晚 23:00 自动关机，开启开机自启、系统托盘与背景图。适合大多数用户。", "每晚 23:00 自動關機，開啟開機自啟、系統匣與背景圖。適合大多數使用者。", "Auto shutdown at 23:00, enable startup, tray and background image. Suitable for most users.", "毎晩 23:00 に自動シャットダウン。自動起動・トレイ・背景画像を有効にします。ほとんどのユーザーに最適。", "매일 밤 23:00 자동 종료, 자동 시작·트레이·배경 이미지 사용. 대부분의 사용자에게 적합.");
            Add("注册表", "登錄檔", "Registry", "レジストリ", "레지스트리");
            Add("电源", "電源", "Power", "電源", "전원");
            Add("硬件", "硬體", "Hardware", "ハードウェア", "하드웨어");
            Add("磁盘", "磁碟", "Disk", "ディスク", "디스크");
            Add("磁盘管理", "磁碟管理", "Disk Management", "ディスクの管理", "디스크 관리");
            Add("程序和功能", "程式和功能", "Programs and Features", "プログラムと機能", "프로그램 및 기능");
            Add("系统", "系統", "System", "システム", "시스템");
            Add("系统信息", "系統資訊", "System Information", "システム情報", "시스템 정보");
            Add("网络", "網路", "Network", "ネットワーク", "네트워크");
            Add("计算器", "計算機", "Calculator", "電卓", "계산기");
            Add("记事本", "記事本", "Notepad", "メモ帳", "메모장");
            Add("设备管理器", "裝置管理員", "Device Manager", "デバイスマネージャー", "장치 관리자");
            Add("调整程序的界面主题", "調整程式的介面主題", "Adjust the app's theme", "アプリのテーマを調整", "앱 테마 조정");
            Add("选择图片", "選擇圖片", "Choose Image", "画像を選択", "이미지 선택");
            Add("默认应用", "預設應用", "Default Apps", "既定のアプリ", "기본 앱");
            Add("定时关机页的开关和时间选择将被禁用", "定時關機頁的開關和時間選擇將被停用", "Scheduled Shutdown toggles and time pickers will be disabled", "シャットダウンページのスイッチと時刻選択が無効になります", "예약 종료 페이지의 스위치와 시간 선택이 비활성화됩니다");
            Add("开启后，对应功能的设置将被锁定，用户无法在界面上修改。请谨慎使用。", "開啟後，對應功能的設定將被鎖定，使用者無法在介面上修改。請謹慎使用。", "When enabled, the corresponding settings are locked and cannot be changed in the UI. Use with caution.", "有効にすると該当機能の設定がロックされ、UI から変更できなくなります。注意して使用してください。", "활성화하면 해당 기능 설정이 잠겨 UI에서 변경할 수 없습니다. 주의해서 사용하세요.");
            Add("托盘开关将被禁用", "系統匣開關將被停用", "Tray toggle will be disabled", "トレイのスイッチが無効になります", "트레이 스위치가 비활성화됩니다");
            Add("用户卡片上的编辑按钮将被禁用", "使用者卡片上的編輯按鈕將被停用", "The edit button on the user card will be disabled", "ユーザーカードの編集ボタンが無効になります", "사용자 카드의 편집 버튼이 비활성화됩니다");
            Add("透明度滑块将被禁用", "透明度滑桿將被停用", "Opacity slider will be disabled", "透過度スライダーが無効になります", "투명도 슬라이더가 비활성화됩니다");
            Add("背景图开关和选择按钮将被禁用", "背景圖開關和選擇按鈕將被停用", "Background image toggle and choose button will be disabled", "背景画像のスイッチと選択ボタンが無効になります", "배경 이미지 스위치와 선택 버튼이 비활성화됩니다");
            Add("重置教程按钮将被禁用", "重設教學按鈕將被停用", "Reset tutorials button will be disabled", "チュートリアルリセットボタンが無効になります", "튜토리얼 초기화 버튼이 비활성화됩니다");
            Add("提示：锁定仅在本程序界面内有效，用户仍可通过手动编辑 config.json 修改设置。", "提示：鎖定僅在本程式介面內有效，使用者仍可透過手動編輯 config.json 修改設定。", "Note: locks only apply within this app; users can still edit config.json manually.", "注意：ロックはこのアプリ内でのみ有効です。config.json を手動編集すれば変更可能です。", "참고: 잠금은 이 앱 내에서만 유효하며 config.json을 수동 편집해 변경할 수 있습니다.");
            Add("配置已保存。你可以随时通过左侧导航切换功能页面。", "設定已儲存。你可以隨時透過左側導覽切換功能頁面。", "Configuration saved. Switch pages anytime via the left navigation.", "設定を保存しました。左のナビゲーションでいつでもページを切り替えられます。", "설정이 저장되었습니다. 왼쪽 탐색에서 언제든 페이지를 전환할 수 있습니다.");
            Add("查询中…", "查詢中…", "Querying…", "照会中…", "조회 중…");
            Add("已开启", "已開啟", "Enabled", "有効", "사용 중");
            Add("未开启", "未開啟", "Disabled", "無効", "사용 안 함");
            Add("最新版本", "最新版本", "Latest", "最新", "최신 버전");
            Add("未知大小", "未知大小", "Unknown size", "サイズ不明", "알 수 없는 크기");
            Add("目标版本：", "目標版本：", "Target version:", "対象バージョン:", "대상 버전:");
            Add("包大小：", "封裝大小：", "Package size:", "パッケージサイズ:", "패키지 크기:");
            Add("渠道", "管道", "channel", "チャネル", "채널");
            Add("更新完成", "更新完成", "Update Complete", "アップデート完了", "업데이트 완료");
            Add("天", "天", "d", "日", "일");
            Add("小时", "小時", "h", "時間", "시간");
            Add("分", "分", "min", "分", "분");
            Add("秒", "秒", "s", "秒", "초");
            Add("项", "項", "items", "件", "개");
            Add("个进程", "個處理程序", "processes", "プロセス", "개 프로세스");
            Add("当前占用", "目前占用", "In use", "使用中", "현재 사용");
            Add("开机时长：", "開機時長：", "Uptime:", "稼働時間:", "가동 시간:");
            Add("线程", "執行緒", "threads", "スレッド", "스레드");
            Add("当前用户", "目前使用者", "Current user", "現在のユーザー", "현재 사용자");
            Add("所有用户", "所有使用者", "All users", "すべてのユーザー", "모든 사용자");
            Add("启动文件夹", "啟動資料夾", "Startup folder", "スタートアップフォルダー", "시작 폴더");
        }

        private static void Add(string zh, string tw, string en, string ja, string ko)
        {
            Dict[zh] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [ZhTW] = tw,
                [En] = en,
                [Ja] = ja,
                [Ko] = ko
            };
        }
    }
}
