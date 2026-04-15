// Source - https://stackoverflow.com/a/74808544
// Posted by zmaplex
// Retrieved 2026-04-14, License - CC BY-SA 4.0

// import { loader , Editor} from "@monaco-editor/react";
// import * as monaco from "monaco-editor";
// loader.config({ monaco });

import React, { useState, useRef } from "react";
import MonacoEditor from "react-monaco-editor";

export default function Merger() {
  const [code, setCode] = useState("// type your code...");
  const editorRef = useRef(null);

  const handleEditorDidMount = (editor, monaco) => {
    console.log("editorDidMount", editor);
    editorRef.current = editor;
    editor.focus();
  };

  const handleChange = (newValue, e) => {
    console.log("onChange", newValue, e);
    setCode(newValue);
  };

  const options = {
    selectOnLineNumbers: true,
  };

  return (
    <div style={{ padding: "20px" }}>
      <h2>Merger Page</h2>

      <MonacoEditor
        width="600"
        height="600"
        language="javascript"
        theme="vs-dark"
        value={code}
        options={options}
        onChange={handleChange}
        editorDidMount={handleEditorDidMount}
      />
    </div>
  );
}