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
            
            // Fallback: create a simple text area if Monaco fails
            const editorElement = document.getElementById('editor');
            if (editorElement) {
                editorElement.innerHTML = `
                    <textarea style="width: 100%; height: 100%; border: none; padding: 10px; font-family: monospace; font-size: 14px; resize: none;" 
                              oninput="window.updateCodeFromFallback(this.value)">
                        ${initialCode}
                    </textarea>
                `;
                window.updateCodeFromFallback = (code) => {
                    dotNetHelper.invokeMethodAsync('UpdateCode', code);
                };
                console.log('Fallback text area created');
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
        // Fallback for text area
        const textarea = document.querySelector('#editor textarea');
        if (textarea) {
            textarea.value = code;
        }
    }
};