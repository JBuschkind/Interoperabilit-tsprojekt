import React, { useState, useRef, useEffect } from "react";
import { MonacoDiffEditor } from "react-monaco-editor";

export default function Merger() {
  const [originalCode] = useState(
    "// your original code...\nfunction a() { return 1; }\n// your original code"
  );

  const [modifiedCode, setModifiedCode] = useState(
    "// modified version...\nfunction a() { return 1; }\n// modified version..."
  );

  const diffEditorRef = useRef(null);
  const monacoRef = useRef(null);

  // ✅ IMPORTANT: separate decoration states
  const modifiedDecorationsRef = useRef([]);
  const originalDecorationsRef = useRef([]);

  const handleEditorDidMount = (diffEditor, monaco) => {
    diffEditorRef.current = diffEditor;
    monacoRef.current = monaco;

    const modifiedEditor = diffEditor.getModifiedEditor();
    const originalEditor = diffEditor.getOriginalEditor();

    // =========================
    // RIGHT SIDE CLICK
    // =========================
    modifiedEditor.onMouseDown((e) => {
      if (
        e.target.type === monaco.editor.MouseTargetType.GUTTER_GLYPH_MARGIN
      ) {
        const line = e.target.position?.lineNumber;
        alert(`RIGHT button clicked at line ${line}`);
      }
    });

    // =========================
    // LEFT SIDE CLICK
    // =========================
    originalEditor.onMouseDown((e) => {
      if (
        e.target.type === monaco.editor.MouseTargetType.GUTTER_GLYPH_MARGIN
      ) {
        const line = e.target.position?.lineNumber;
        alert(`LEFT button clicked at line ${line}`);
      }
    });

    diffEditor.getOriginalEditor().updateOptions({ glyphMargin: true });
    diffEditor.getModifiedEditor().updateOptions({ glyphMargin: true });

    updateDiffDecorations();
  };

  const handleChange = (value) => {
    setModifiedCode(value);
  };

  const updateDiffDecorations = () => {
    const diffEditor = diffEditorRef.current;
    const monaco = monacoRef.current;

    if (!diffEditor || !monaco) return;

    const modifiedEditor = diffEditor.getModifiedEditor();
    const originalEditor = diffEditor.getOriginalEditor();

    const changes = diffEditor.getLineChanges?.() || [];

    // =========================
    // RIGHT SIDE (modified)
    // =========================
    const modifiedDecorations = changes.map((change) => ({
      range: new monaco.Range(
        change.modifiedStartLineNumber,
        1,
        change.modifiedStartLineNumber,
        1
      ),
      options: {
        glyphMarginClassName: "diff-glyph-button",
        glyphMarginHoverMessage: { value: "Apply / revert change" },
      },
    }));

    // =========================
    // LEFT SIDE (original)
    // =========================
    const originalDecorations = changes.map((change) => ({
      range: new monaco.Range(
        change.originalStartLineNumber,
        1,
        change.originalStartLineNumber,
        1
      ),
      options: {
        glyphMarginClassName: "diff-glyph-button-left",
        glyphMarginHoverMessage: { value: "Original side action" },
      },
    }));

    // APPLY + PERSIST (this is the key fix)
    modifiedDecorationsRef.current = modifiedEditor.deltaDecorations(
      modifiedDecorationsRef.current,
      modifiedDecorations
    );

    originalDecorationsRef.current = originalEditor.deltaDecorations(
      originalDecorationsRef.current,
      originalDecorations
    );
  };

  // Update decorations when code changes
  useEffect(() => {
    const timer = setTimeout(() => {
      updateDiffDecorations();
    }, 100);

    return () => clearTimeout(timer);
  }, [modifiedCode]);

  const options = {
    renderSideBySide: true,
    useInlineViewWhenSpaceIsLimited: false,
    readOnly: false,
    glyphMargin: true,
    renderIndicators: false,
    lineDecorationsWidth: 20,
  };

  return (
    <div style={{ padding: "20px", width: "100%" }}>
      <h2>Merger Page</h2>

      <MonacoDiffEditor
        width="800px"
        height="400px"
        language="javascript"
        original={originalCode}
        value={modifiedCode}
        options={options}
        onChange={handleChange}
        editorDidMount={handleEditorDidMount}
      />

      {/* Button styling */}
      <style>{`
        .diff-glyph-button {
          background: #4caf50;
          width: 12px !important;
          height: 12px !important;
          border-radius: 3px;
          cursor: pointer;
          margin-left: 3px;
        }

        .diff-glyph-button:hover {
          background: #2e7d32;
        }

        .diff-glyph-button-left {
          background: #ff9800;
          width: 12px !important;
          height: 12px !important;
          border-radius: 3px;
          cursor: pointer;
          margin-left: 3px;
        }

        .diff-glyph-button-left:hover {
          background: #ef6c00;
        }
      `}</style>
    </div>
  );
}