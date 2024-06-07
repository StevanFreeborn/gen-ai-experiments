<script setup lang="ts">
  import { computed, ref } from 'vue';

  const props = defineProps<{
    show: boolean;
  }>();

  const emit = defineEmits<{
    close: [];
  }>();

  const prompt = ref('');
  const output = ref('');
  const textArea = ref<HTMLTextAreaElement | null>(null);
  const isSubmitting = ref(false);

  const sidebarClasses = computed(() => {
    return {
      'sidebar-container': true,
      show: props.show,
    };
  });

  function handleCloseButtonClick() {
    emit('close');
  }

  function handlePromptInput(event: Event) {
    const target = event.target as HTMLTextAreaElement;
    prompt.value = target.value;

    if (prompt.value.includes('\n') === false) {
      textArea.value.style.height = '2rem';
      return;
    }

    textArea.value.style.height = 'auto';
    const scrollHeight = textArea.value.scrollHeight;

    if (scrollHeight > 300) {
      textArea.value.style.height = '300px';
      return;
    }

    textArea.value.style.height = `${scrollHeight}px`;
  }

  function handlePromptKeyDown(event: KeyboardEvent) {
    if (event.key === 'Enter' && event.shiftKey === true) {
      event.preventDefault();
      // TODO: Handle submit
      return;
    }
  }

  async function handleSubmit() {
    isSubmitting.value = true;

    if (prompt.value.trim().length === 0) {
      isSubmitting.value = false;
      return;
    }

    await new Promise(resolve => setTimeout(resolve, 2000));

    output.value = '<h1>Response</h1><p>This is a response to your prompt.</p>';
    prompt.value = '';
    isSubmitting.value = false;
  }
</script>

<template>
  <div :class="sidebarClasses">
    <div class="sidebar">
      <div class="action-container">
        <button type="button" class="accept-button" v-if="output">Accept</button>
        <button type="button" class="close-button" @click="handleCloseButtonClick">Close</button>
      </div>
      <div class="response-container">
        <div v-html="output"></div>
      </div>
      <div class="prompt-container">
        <label for="prompt" class="sr-only">Prompt</label>
        <textarea
          ref="textArea"
          id="prompt"
          name="prompt"
          class="prompt-input"
          autofocus="true"
          placeholder="What would you like help with?"
          :value="prompt"
          @input="handlePromptInput"
          @keydown="handlePromptKeyDown"
        ></textarea>
        <button
          type="button"
          :disabled="prompt.trim().length === 0 || isSubmitting"
          @click="handleSubmit"
        >
          <span class="loader" v-if="isSubmitting">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" fill="currentColor">
              <path
                d="M222.7 32.1c5 16.9-4.6 34.8-21.5 39.8C121.8 95.6 64 169.1 64 256c0 106 86 192 192 192s192-86 192-192c0-86.9-57.8-160.4-137.1-184.1c-16.9-5-26.6-22.9-21.5-39.8s22.9-26.6 39.8-21.5C434.9 42.1 512 140 512 256c0 141.4-114.6 256-256 256S0 397.4 0 256C0 140 77.1 42.1 182.9 10.6c16.9-5 34.8 4.6 39.8 21.5z"
              />
            </svg>
            <span class="sr-only">Submitting</span>
          </span>
          <span v-else>
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 384 512" fill="currentColor">
              <path
                d="M214.6 41.4c-12.5-12.5-32.8-12.5-45.3 0l-160 160c-12.5 12.5-12.5 32.8 0 45.3s32.8 12.5 45.3 0L160 141.2V448c0 17.7 14.3 32 32 32s32-14.3 32-32V141.2L329.4 246.6c12.5 12.5 32.8 12.5 45.3 0s12.5-32.8 0-45.3l-160-160z"
              />
            </svg>
            <span class="sr-only">Submit</span>
          </span>
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
  .sidebar-container {
    z-index: 1000;
    position: absolute;
    top: 0;
    right: 0;
    display: flex;
    flex-direction: column;
    height: 100%;
    width: 0;
    background-color: var(--bg-color);
    transition: width 0.5s ease-in-out;
    overflow: hidden;
  }

  .show {
    width: 100%;
  }

  .sidebar {
    display: flex;
    flex-direction: column;
    width: 100%;
    height: 100%;
    padding: 1rem;
    gap: 1rem;
  }

  .action-container {
    display: flex;
    width: 100%;
    justify-content: flex-end;
    gap: 1rem;

    button {
      padding: 0.5rem 1rem;
      border: 1px solid var(--border-color);
      border-radius: 0.5rem;
      color: var(--text-color);
      cursor: pointer;
    }

    .accept-button {
      background-color: var(--success-color);
    }

    .close-button {
      background-color: var(--danger-color);
    }
  }

  .response-container {
    flex: 1;
    padding: 1rem;
    border: 1px solid var(--border-color);
    border-radius: 0.5rem;
    overflow-y: auto;
  }

  .prompt-container {
    display: flex;
    gap: 1rem;
    align-items: flex-end;
    padding: 0.5rem;
    background-color: var(--bg-color);
    border-radius: 0.5rem;
    border: 1px solid var(--border-color);

    &:focus {
      outline: 2px solid var(--border-color);
    }

    textarea {
      flex: 1;
      border: none;
      border-radius: 0.5rem;
      resize: none;
      font-size: 1rem;
      line-height: 1rem;
      color: var(--text-color);
      background-color: var(--bg-color);
      padding: 0.5rem 0;
      height: 2rem;
      scrollbar-color: var(--border-color) var(--bg-color);
    }

    textarea:focus-visible {
      outline: none;
    }

    button {
      height: max-content;
      padding: 0.5rem 1rem;
      border: 1px solid var(--border-color);
      border-radius: 0.5rem;
      color: var(--text-color);
      cursor: pointer;
      background-color: var(--primary-color);
      display: flex;
      align-items: center;
      justify-content: center;

      & span {
        display: flex;
        align-items: center;
        justify-content: center;

        & svg {
          width: 1rem;
          height: 1rem;
        }
      }

      .loader svg {
        animation: spin 2s linear infinite;
      }
    }

    button:disabled {
      background-color: var(--bg-color);
    }
  }

  @keyframes spin {
    0% {
      transform: rotate(0deg);
    }
    100% {
      transform: rotate(360deg);
    }
  }
</style>
