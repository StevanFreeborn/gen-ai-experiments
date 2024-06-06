<script setup>
import 'tinymce';

/* Required TinyMCE components */
import 'tinymce/icons/default';
import 'tinymce/themes/silver';
import 'tinymce/models/dom/model';

/* Import a skin (can be a custom skin instead of the default) */
import 'tinymce/skins/ui/tinymce-5/skin.css';

import 'tinymce/skins/ui/tinymce-5-dark/skin.css';

import Editor from '@tinymce/tinymce-vue';
import { ref } from 'vue';

const editorValue = ref('');
const suggestion = ref('');
const enablePredictiveText = ref(true);

function createTypingHandlers(delay, callback) {
  let typingTimer;

  function handleKeyUp(event, editor) {
    if (editor === undefined) {
      return;
    }

    clearTimeout(typingTimer);

    const blacklist = [
      'ArrowLeft',
      'ArrowRight',
      'ArrowUp',
      'ArrowDown',
      'Escape',
      'Tab',
      'Control',
      'Alt',
      'Meta',
    ];

    if (blacklist.includes(event.key)) {
      return;
    }

    typingTimer = setTimeout(() => doneTyping(event, editor), delay);
  }

  function handleKeyDown(event, editor) {
    if (editor === undefined) {
      return;
    }

    clearTimeout(typingTimer);

    if (!suggestion.value) {
      return;
    }

    const firstChar = suggestion.value[0];
    const suggestionNode = editor.dom.get('ai-suggestion');

    if (event.key === 'Shift') {
      return;
    }

    if (event.key === 'Tab') {
      event.preventDefault();
      const suggestionNode = editor.dom.get('ai-suggestion');
      suggestionNode.remove();

      editor.execCommand('mceInsertContent', false, suggestion.value);

      suggestion.value = '';
      return;
    }

    if (event.key === 'Escape' || event.key !== firstChar) {
      suggestionNode.remove();
      suggestion.value = '';
      editor.execCommand('mceInsertContent', false, '');

      return;
    }

    suggestion.value = suggestion.value.slice(1);
    suggestionNode.textContent = suggestion.value;
  }

  function doneTyping(event, editor) {
    callback(event, editor);
  }

  return {
    handleKeyUp,
    handleKeyDown,
  };
}

const { handleKeyUp, handleKeyDown } = createTypingHandlers(1000, async (event, editor) => {
  if (!editorValue.value || suggestion.value || enablePredictiveText.value === false) {
    return;
  }

  const res = await fetch(`${import.meta.env.VITE_API_URL}/complete`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ input: editorValue.value }),
  });

  if (!res.ok) {
    return;
  }

  const data = await res.json();
  const suggestionText = data.response;

  const { anchorNode, anchorOffset } = editor.selection.getSel();

  suggestion.value = suggestionText;

  const suggestionNode = editor.dom.get('ai-suggestion');

  if (suggestionNode) {
    suggestionNode.remove();
  }

  editor.execCommand(
    'mceInsertContent',
    false,
    `<span id="ai-suggestion" style="opacity: 0.7;">${suggestionText}</span>`
  );

  editor.selection.setCursorLocation(anchorNode, anchorOffset);
});

function handleBlur(event, editor) {
  if (editor === undefined) {
    return;
  }

  if (!suggestion.value) {
    return;
  }

  const suggestionNode = editor.dom.get('ai-suggestion');
  suggestionNode.remove();
  suggestion.value = '';
}

const isDarkMode = window.matchMedia('(prefers-color-scheme: dark)').matches;

const brainIcon =
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512"><!--!Font Awesome Free 6.5.2 by @fontawesome - https://fontawesome.com License - https://fontawesome.com/license/free Copyright 2024 Fonticons, Inc.--><path d="M184 0c30.9 0 56 25.1 56 56V456c0 30.9-25.1 56-56 56c-28.9 0-52.7-21.9-55.7-50.1c-5.2 1.4-10.7 2.1-16.3 2.1c-35.3 0-64-28.7-64-64c0-7.4 1.3-14.6 3.6-21.2C21.4 367.4 0 338.2 0 304c0-31.9 18.7-59.5 45.8-72.3C37.1 220.8 32 207 32 192c0-30.7 21.6-56.3 50.4-62.6C80.8 123.9 80 118 80 112c0-29.9 20.6-55.1 48.3-62.1C131.3 21.9 155.1 0 184 0zM328 0c28.9 0 52.6 21.9 55.7 49.9c27.8 7 48.3 32.1 48.3 62.1c0 6-.8 11.9-2.4 17.4c28.8 6.2 50.4 31.9 50.4 62.6c0 15-5.1 28.8-13.8 39.7C493.3 244.5 512 272.1 512 304c0 34.2-21.4 63.4-51.6 74.8c2.3 6.6 3.6 13.8 3.6 21.2c0 35.3-28.7 64-64 64c-5.6 0-11.1-.7-16.3-2.1c-3 28.2-26.8 50.1-55.7 50.1c-30.9 0-56-25.1-56-56V56c0-30.9 25.1-56 56-56z"/></svg>';
</script>

<template>
  <main>
    <div id="container" class="container">
      <Editor
        class="textarea"
        api-key="no-api-key"
        :init="{
          toolbar:
            'undo redo | styles | bold italic | alignleft aligncenter alignright alignjustify | outdent indent | disablePredictiveText',
          menubar: false,
          promotion: false,
          branding: false,
          resize: false,
          extended_valid_elements: 'span[*]',
          toolbar_mode: 'scrolling',
          auto_focus: true,
          skin: isDarkMode ? 'tinymce-5-dark' : 'tinymce-5',
          setup: editor => {
            editor.ui.registry.addIcon('brain', brainIcon);

            editor.ui.registry.addToggleButton('disablePredictiveText', {
              text: '🧠',
              onSetup: api => {
                api.setActive(enablePredictiveText);
              },
              onAction: api => {
                enablePredictiveText = !enablePredictiveText;
                api.setActive(enablePredictiveText);
              },
            });
          },
        }"
        :inline="true"
        output-format="text"
        v-model="editorValue"
        @keyup="handleKeyUp"
        @keydown="handleKeyDown"
        @blur="handleBlur"
      />
    </div>
  </main>
</template>

<style scoped>
main {
  height: 100%;
  padding: 3rem 1rem;
}

.container {
  width: 100%;
  height: 100%;
  position: relative;
}

.textarea {
  position: relative;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  padding: 0.75rem;
  width: 100%;
  height: 100%;
  resize: none;
  background-color: var(--color-bg);
}

.container-mirror {
  position: absolute;
  top: 0;
  left: 0;
  height: 100%;
  width: 100%;
  overflow: hidden;
  color: red;
}

.ai-suggestion {
  opacity: 0.5;
}
</style>
