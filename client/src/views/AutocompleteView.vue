<script setup>
import 'tinymce';
import bloopSound from '@/assets/bloop.mp3';

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
const audio = new Audio(bloopSound);

function playBloopSound() {
  audio.currentTime = 1.55;
  audio.play();
}

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

  playBloopSound();

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
            editor.ui.registry.addToggleButton('disablePredictiveText', {
              icon: 'ai',
              onSetup: api => {
                api.setActive(enablePredictiveText);
              },
              onAction: api => {
                playBloopSound();
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
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  padding: 0.75rem;
  width: 100%;
  height: 100%;
  resize: none;
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
