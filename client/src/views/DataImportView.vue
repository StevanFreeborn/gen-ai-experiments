<script setup lang="ts">
  import { ref } from 'vue';

  const response = ref<string | null>(null);

  function validateCsv(csvData: string, hasHeader: boolean) {
    const rows = csvData.split('\n');

    if (!csvData.trim()) {
      alert('The file is empty. Expected CSV data.');
      return;
    }

    if (hasHeader) {
      const header = rows[0].split(',');

      for (let i = 1; i < rows.length; i++) {
        const row = rows[i].split(',');

        if (header.length !== row.length) {
          alert(
            `File is invalid. The number of columns in the header does not match the number of columns in the data row. ${i}`
          );
          return;
        }
      }
    }
  }

  async function handleSubmit(event: Event) {
    const formDate = new FormData(event.target as HTMLFormElement);
    const importFile = formDate.get('importFile');
    const hasHeader = formDate.get('hasHeader') === 'on';

    if (importFile instanceof File) {
      if (importFile.type !== 'text/csv') {
        alert('Invalid file type. Please upload a CSV file.');
        return;
      }

      const reader = new FileReader();

      reader.onload = event => {
        if (event.target !== null && typeof event.target.result === 'string') {
          validateCsv(event.target.result, hasHeader);
        }
      };

      reader.readAsText(importFile);

      const uploadResponse = await fetch(`${import.meta.env.VITE_API_URL}/analyze-import`, {
        method: 'POST',
        body: formDate,
      });

      if (uploadResponse.ok === false) {
        return;
      }

      const json = await uploadResponse.json();
      response.value = json;
    }
  }
</script>

<template>
  <main>
    <form @submit.prevent="handleSubmit" v-if="response === null">
      <div class="form-col-group">
        <label for="importFile">Import File:</label>
        <input type="file" name="importFile" id="importFile" accept=".csv" />
      </div>
      <div class="form-row-group">
        <input type="checkbox" name="hasHeader" id="hasHeader" checked />
        <label for="hasHeader">File has header row</label>
      </div>
      <button type="submit">Upload</button>
    </form>
    <div v-else class="response-container">
      <pre>{{ response }}</pre>
    </div>
  </main>
</template>

<style scoped>
  main {
    display: flex;
    flex-direction: column;
    align-items: center;
    margin: 1rem;
    height: 100%;
  }

  form {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    flex: 1;

    & button[type='submit'] {
      padding: 0.5rem 1rem;
      background-color: var(--primary-color);
      color: white;
      border: none;
      border-radius: 0.25rem;
      cursor: pointer;
    }
  }

  .form-col-group {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;

    & label {
      font-weight: bold;
    }
  }

  .form-row-group {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .response-container {
    padding: 1rem;
    border: 1px solid var(--primary-color);
    border-radius: 0.25rem;
    background-color: var(--primary-color-light);
  }
</style>
