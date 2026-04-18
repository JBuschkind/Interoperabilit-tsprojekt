import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { DefaultDarkColors, DefaultLightColors, MisMerge3 } from '@mismerge/react';
import { codeToHtml } from 'shiki';
import '@mismerge/core/styles.css';
import '@mismerge/core/dark.css';

export default function NewMerger() {

  const navigate = useNavigate();


  const [ctr, setCtr] = useState('');

  useEffect(() => {
    console.log(ctr);
  }, [ctr]);


	const highlight = async (text: string) =>
		await codeToHtml(text, {
			lang: "js",
			theme: 'github-dark'
		});

  const originalCode = `function greet(name) {
    console.log("Hello, " + name);
    return name;
}

const user = "World";
greet(user);

//some other code that should not be removed

//hidden`;

  const modifiedCode = `function greet(name) {
    console.log("Hello, " + name + "!");
    return \`Greeted: \${name}\`;
}
const newVariable = "This is a new variable";
const newVariable2 = "This is a new variable2";

const user = "World";
const result = greet(user);
console.log(result);

//hidden`;



  return (
    <div style={{  width: "95vw" }}>

      <style>
        {`
          .mismerge {
            font-family: 'Fira Code', monospace;
            font-variant-ligatures: normal;
            min-height: 80vh;
            margin-top: 1rem;
          }

          .shiki {
            background-color: transparent !important;
          }
        `}
      </style>

          <div style={{ border: "1px solid #ccc", borderRadius: "4px", width: "100%", height:"30px", display: "flex", alignItems: "center",  justifyContent: "space-around"}}>
            <div>Original</div>
            <div>Current</div>
            <div>New</div>
          </div>

          <MisMerge3
            lhs={originalCode}
            ctr={ctr}
            rhs={modifiedCode}
            onCtrChange={setCtr}
            colors={DefaultDarkColors}
            wrapLines={true}
            highlight={highlight}
          />

        <button style={{ margin: "5px" }} onClick={() => navigate('/')}>
            Go to Merger
        </button>
    </div>
  );
}