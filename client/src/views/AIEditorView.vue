<script setup lang="ts">
  import 'tinymce';
  import bloopSound from '@/assets/bloop.mp3';

  import 'tinymce/icons/default';
  import 'tinymce/themes/silver';
  import 'tinymce/models/dom/model';

  import 'tinymce/skins/ui/tinymce-5/skin.css';
  import 'tinymce/skins/ui/tinymce-5-dark/skin.css';

  import 'tinymce/plugins/lists';

  import Editor from '@tinymce/tinymce-vue';
  import PromptSidebar from '@/components/PromptSidebar.vue';
  import { ref } from 'vue';

  const isLoading = ref(true);
  const editorValue = ref('');
  const suggestion = ref('');
  const enablePredictiveText = ref<boolean>(true);
  const audio = new Audio(bloopSound);
  const showSidebar = ref(false);
  const editorRef = ref<any>(null);

  function handleEditorInitialized(event: any, editor: any) {
    editorRef.value = editor;
    isLoading.value = false;
  }

  function playBloopSound() {
    audio.currentTime = 1.55;
    audio.play();
  }

  function createTypingHandlers(
    delay: number,
    callback: (event: KeyboardEvent, editor: any) => void
  ) {
    let typingTimer: any;

    function handleKeyUp(event: KeyboardEvent, editor: any) {
      if (editor === undefined) {
        return;
      }

      clearTimeout(typingTimer as number);

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

    function handleKeyDown(event: KeyboardEvent, editor: any) {
      if (editor === undefined) {
        return;
      }

      clearTimeout(typingTimer as number);

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
        suggestionNode.textContent = '';
        suggestion.value = '';
        editor.execCommand('mceInsertContent', false, '');

        return;
      }

      suggestion.value = suggestion.value.slice(1);
      suggestionNode.textContent = suggestion.value;
    }

    function doneTyping(event: KeyboardEvent, editor: any) {
      callback(event, editor);
    }

    return {
      handleKeyUp,
      handleKeyDown,
    };
  }

  const { handleKeyUp, handleKeyDown } = createTypingHandlers(1000, async (event, editor) => {
    if (
      editorValue.value.trim().length === 0 ||
      suggestion.value ||
      enablePredictiveText.value === false
    ) {
      return;
    }

    const originalEditorValue = editorValue.value;

    const res = await fetch(`${import.meta.env.VITE_API_URL}/complete`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json; charset=UTF-8',
      },
      body: JSON.stringify({ input: originalEditorValue }),
    });

    // If the editor value has changed since the fetch request was made, then we don't want to show the suggestion
    if (editorValue.value !== originalEditorValue) {
      return;
    }

    // If the selection is not collapsed, then we don't want to show the suggestion
    // This is to prevent the suggestion from being inserted when the user is selecting text
    if (editor.selection.isCollapsed() === false) {
      return;
    }

    if (!res.ok) {
      return;
    }

    const data = await res.json();
    const suggestionText = data.response;

    if (!suggestionText) {
      return;
    }

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

  function removeSuggestion(editor: any) {
    if (editor === undefined) {
      return;
    }

    const suggestionNode = editor.dom.get('ai-suggestion');
    suggestionNode.textContent = '';
    suggestion.value = '';
  }

  function handleBlur(event: FocusEvent, editor: any) {
    if (!suggestion.value) {
      return;
    }

    removeSuggestion(editor);
  }

  const isDarkMode = window.matchMedia('(prefers-color-scheme: dark)').matches;

  function handlePromptSidebarClose() {
    showSidebar.value = false;
    editorRef.value.focus();
  }

  function handleAcceptPromptResponse(output: string) {
    if (editorRef.value === null) {
      return;
    }

    editorRef.value.setContent(output);
    editorRef.value.focus();
    playBloopSound();
  }
</script>

<template>
  <PromptSidebar
    :show="showSidebar"
    @close="handlePromptSidebarClose"
    @accept="handleAcceptPromptResponse"
  />
  <main>
    <div id="container" class="container">
      <div class="loading-message" :style="{ display: isLoading ? 'flex' : 'none' }">
        <p>Loading...</p>
      </div>
      <Editor
        class="textarea"
        :style="{ display: isLoading ? 'none' : 'block' }"
        api-key="no-api-key"
        :init="{
          plugins: 'lists',
          lists_indent_on_tab: false,
          toolbar:
            'undo redo | styles | bold italic | alignleft aligncenter alignright alignjustify | outdent indent | bullist numlist | disablePredictiveText showPromptSidebar',
          menubar: false,
          promotion: false,
          branding: false,
          resize: false,
          extended_valid_elements: 'span[*]',
          toolbar_mode: 'scrolling',
          auto_focus: true,
          skin: isDarkMode ? 'tinymce-5-dark' : 'tinymce-5',
          setup: (editor: any) => {
            editor.ui.registry.addToggleButton('disablePredictiveText', {
              icon: 'ai',
              onSetup: (api: any) => {
                api.setActive(enablePredictiveText);
              },
              onAction: (api: any) => {
                playBloopSound();
                enablePredictiveText = !enablePredictiveText;
                api.setActive(enablePredictiveText);
              },
            });

            editor.ui.registry.addToggleButton('showPromptSidebar', {
              icon: 'ai-prompt',
              onAction: () => {
                showSidebar = !showSidebar;
              },
            });
          },
        }"
        :inline="true"
        output-format="text"
        v-model="editorValue"
        @init="handleEditorInitialized"
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
  }

  .loading-message {
    justify-content: center;
    align-items: center;
    height: 100%;
  }

  .textarea {
    position: relative;
    border: 1px solid var(--border-color);
    border-radius: 0.5rem;
    padding: 0.75rem;
    width: 100%;
    height: 100%;
    resize: none;
    overflow: auto;
    scrollbar-color: var(--border-color) var(--bg-color);
  }

  .ai-suggestion {
    opacity: 0.5;
  }
</style>

<style>
  .mce-content-body ul {
    list-style-type: disc;
    margin-left: 1rem;
  }

  .mce-content-body ol {
    list-style-type: decimal;
    margin-left: 1rem;
  }
</style>
