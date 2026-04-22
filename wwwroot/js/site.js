window.initializeMonacoEditor = async (dotNetHelper, initialCode) => {
    const initEditor = () => {
        const editorElement = document.getElementById('editor');
        if (!editorElement) {
            console.error('Editor element not found');
            return;
        }

        console.log('Initializing Monaco Editor...');
        
        try {
            if (!window.monaco) {
                console.error('Monaco is not available');
                return;
            }

            const editor = window.monaco.editor.create(editorElement, {
                value: initialCode,
                language: 'csharp',
                theme: 'hc-light',
                minimap: { enabled: false },
                fontSize: 14,
                lineNumbers: 'on',
                roundedSelection: false,
                scrollBeyondLastLine: false,
                automaticLayout: true
            });

            window.monacoEditor = editor;

            editor.onDidChangeModelContent(() => {
                const code = editor.getValue();
                dotNetHelper.invokeMethodAsync('UpdateCode', code);
            });

            console.log('Monaco Editor initialized successfully');
        } catch (error) {
            console.error('Failed to initialize Monaco Editor:', error);
        }
    };

    const waitForMonaco = (attempts = 0) => {
        if (window.monaco && window.monacoEditorLoaded) {
            initEditor();
        } else if (attempts < 100) {
            setTimeout(() => waitForMonaco(attempts + 1), 100);
        } else {
            console.error('Monaco Editor not available after timeout');
            
            // Show popup message instead of textarea fallback
            const editorElement = document.getElementById('editor');
            if (editorElement) {
                editorElement.innerHTML = `
                    <div style="width: 100%; height: 100%; display: flex; align-items: center; justify-content: center; background-color: #f8f9fa; border: 2px dashed #dee2e6; border-radius: 8px;">
                        <div style="text-align: center; padding: 20px; max-width: 400px;">
                            <h4 style="color: #dc3545; margin-bottom: 15px;">⚠️ Monaco Editor Not Available</h4>
                            <p style="color: #6c757d; margin-bottom: 20px;">
                                The Monaco Editor failed to load. Please refresh the page to try again.
                            </p>
                            <button onclick="location.reload()" style="background-color: #007bff; color: white; border: none; padding: 10px 20px; border-radius: 4px; cursor: pointer;">
                                Refresh Page
                            </button>
                        </div>
                    </div>
                `;
                console.log('Monaco Editor popup message displayed');
            }
        }
    };

    waitForMonaco();
};

window.setEditorContent = (code) => {
    if (window.monacoEditor) {
        window.monacoEditor.setValue(code);
    } else if (window.monacoEditorLoaded) {
        // Editor not yet initialized but Monaco is loaded
        setTimeout(() => window.setEditorContent(code), 100);
    } else {
        // No action needed for popup scenario - user needs to refresh
        console.log('Monaco Editor not available; page refresh required');
    }
};