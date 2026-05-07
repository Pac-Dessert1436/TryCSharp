// Language switching functions
window.currentLanguage = 'en';

window.translations = {
    en: {
        "AppName": "Try C#",
        "AppSubtitle": "Compile and Run C# Code Online",
        "HomePageTitle": "Try C# by Pac-Dessert1436",
        "DescriptionFirst": "Compile and run C# code directly in your browser. A homage to Microsoft's",
        "DescriptionSecond": " tool.",
        "CardTitleCodeEditor": "C# Code Editor",
        "ButtonRunCode": "Run Code",
        "ButtonRunning": "Running...",
        "ButtonReset": "Reset to Default Scaffold",
        "CardTitleFeatures": "Features",
        "Feature1": "Compile C# code using Roslyn on the server",
        "Feature2": "Execute code and see results instantly",
        "Feature3": "Syntax highlighting with Monaco Editor",
        "Feature4": "Built-in examples to get started",
        "Feature5": "Support for multiple C# features",
        "Feature6": "Responsive design for mobile and desktop",
        "Feature7": "Output shown at the page bottom",
        "CardTitleExamples": "Examples",
        "ExampleHelloWorld": "Hello World",
        "ExampleFibonacci": "Fibonacci",
        "ExampleLinq": "LINQ Example",
        "CardTitleOutput": "Output",
        "OutputPlaceholder": "Output will appear here...",
        "GitHubLinkText": "See GitHub Repository",
        "LanguageSwitcherLabel": "Language:",
        "LanguageEnglish": "English",
        "LanguageChinese": "中文"
    },
    "zh-CN": {
        "AppName": "尝试 C#",
        "AppSubtitle": "在线编译和运行 C# 代码",
        "HomePageTitle": "Try C# by Pac-Dessert1436",
        "DescriptionFirst": "直接在浏览器中编译和运行 C# 代码。致敬微软的",
        "DescriptionSecond": " 工具。",
        "CardTitleCodeEditor": "C# 代码编辑器",
        "ButtonRunCode": "运行代码",
        "ButtonRunning": "正在运行...",
        "ButtonReset": "重置为默认模板",
        "CardTitleFeatures": "功能特性",
        "Feature1": "使用服务器端的 Roslyn 编译 C# 代码",
        "Feature2": "执行代码并立即查看结果",
        "Feature3": "Monaco 编辑器提供语法高亮",
        "Feature4": "内置示例帮助入门",
        "Feature5": "支持多种 C# 功能",
        "Feature6": "响应式设计，支持移动和桌面设备",
        "Feature7": "输出显示在页面底部",
        "CardTitleExamples": "示例",
        "ExampleHelloWorld": "你好世界",
        "ExampleFibonacci": "斐波那契数列",
        "ExampleLinq": "LINQ 示例",
        "CardTitleOutput": "输出",
        "OutputPlaceholder": "输出将显示在此处...",
        "GitHubLinkText": "查看 GitHub 仓库",
        "LanguageSwitcherLabel": "语言：",
        "LanguageEnglish": "English",
        "LanguageChinese": "中文"
    }
};

window.switchLanguage = function(language) {
    console.log('Switching language to:', language);
    window.currentLanguage = language;
    
    // Save to localStorage
    localStorage.setItem('culture', language);
    
    // Update all elements with data-translate attribute
    var elements = document.querySelectorAll('[data-translate]');
    console.log('Found', elements.length, 'elements to translate');
    
    elements.forEach(function(element) {
        var key = element.getAttribute('data-translate');
        if (window.translations[language] && window.translations[language][key]) {
            console.log('Translating', key, 'to', window.translations[language][key]);
            element.textContent = window.translations[language][key];
        } else {
            console.log('No translation found for', key);
        }
    });
};

// Initialize language on page load
document.addEventListener('DOMContentLoaded', function() {
    var savedLanguage = localStorage.getItem('culture') || 'en';
    console.log('Initializing language:', savedLanguage);
    if (savedLanguage !== 'en') {
        window.switchLanguage(savedLanguage);
    }
});

// Get translation value for Blazor components
window.getTranslation = function(key) {
    var lang = window.currentLanguage || 'en';
    if (window.translations[lang] && window.translations[lang][key]) {
        return window.translations[lang][key];
    }
    return key;
};