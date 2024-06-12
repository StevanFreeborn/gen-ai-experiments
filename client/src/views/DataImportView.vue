<script setup lang="ts">
  import { ref } from 'vue';

  type ColumnType = 'text' | 'number' | 'date' | 'list';

  type ColumnResult = {
    name: string;
    type: ColumnType;
    description: string;
  };

  type ImportAnalysis = {
    appName: string;
    columns: ColumnResult[];
  };

  const isAnalyzing = ref(false);
  const isCreatingApp = ref(false);
  const isImportingData = ref(false);

  const importFile = ref<File | null>(null);
  const importAnalysis = ref<ImportAnalysis | null>(null);
  const createdAppUrl = ref<string | null>(null);
  const reportUrl = ref<string | null>(null);

  function validateCsv(csvData: string, hasHeader: boolean) {
    const rows = csvData.split('\n');

    if (!csvData.trim()) {
      alert('The file is empty. Expected CSV data.');
      return false;
    }

    if (hasHeader) {
      const header = rows[0].split(',');

      for (let i = 1; i < rows.length; i++) {
        const row = rows[i].split(',');

        if (header.length !== row.length) {
          alert(
            `File is invalid. The number of columns in the header does not match the number of columns in the data row. ${i}`
          );
          return false;
        }
      }
    }

    return true;
  }

  async function handleSubmit(event: Event) {
    isAnalyzing.value = true;
    const formDate = new FormData(event.target as HTMLFormElement);
    const importFileField = formDate.get('importFile');
    const hasHeaderField = formDate.get('hasHeader') === 'on';

    if (importFileField instanceof File) {
      if (importFileField.type !== 'text/csv') {
        alert('Invalid file type. Please upload a CSV file.');
        isAnalyzing.value = false;
        return;
      }

      const reader = new FileReader();

      let isValid = false;

      reader.onload = event => {
        if (event.target !== null && typeof event.target.result === 'string') {
          isValid = validateCsv(event.target.result, hasHeaderField);
        }
      };

      reader.onloadend = async () => {
        if (isValid === false) {
          isAnalyzing.value = false;
          return;
        }

        importFile.value = importFileField;

        const analyzeResponse = await fetch(`${import.meta.env.VITE_API_URL}/analyze-import`, {
          method: 'POST',
          body: formDate,
        });

        if (analyzeResponse.ok === false) {
          isAnalyzing.value = false;
          return;
        }

        const json = await analyzeResponse.json();
        importAnalysis.value = json as ImportAnalysis;
        isAnalyzing.value = false;
      };

      reader.readAsText(importFileField);
    }
  }

  async function handleCreateAppButtonClick() {
    isCreatingApp.value = true;

    const createAppResponse = await fetch(`${import.meta.env.VITE_API_URL}/create-app`, {
      method: 'POST',
      body: JSON.stringify(importAnalysis.value),
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (createAppResponse.ok === false) {
      alert('Failed to create app.');
      isCreatingApp.value = false;
      return;
    }

    const json = await createAppResponse.json();
    createdAppUrl.value = json.url;
    isCreatingApp.value = false;
  }

  async function handleImportDataButtonClick() {
    isImportingData.value = true;

    const formDate = new FormData();

    if (importFile.value === null) {
      alert('No import file selected.');
      isImportingData.value = false;
      return;
    }

    if (importAnalysis.value === null) {
      alert('No import analysis data.');
      isImportingData.value = false;
      return;
    }

    if (createdAppUrl.value === null) {
      alert('No app created.');
      isImportingData.value = false;
      return;
    }

    formDate.append('importFile', importFile.value);
    formDate.append('appUrl', createdAppUrl.value);
    formDate.append(
      'importAnalysisFile',
      new Blob([JSON.stringify(importAnalysis.value)], { type: 'application/json' })
    );

    const importDataResponse = await fetch(`${import.meta.env.VITE_API_URL}/create-import`, {
      method: 'POST',
      body: formDate,
    });

    if (importDataResponse.ok === false) {
      alert('Failed to import data.');
      isImportingData.value = false;
      return;
    }

    const json = await importDataResponse.json();
    reportUrl.value = json.url;
    isImportingData.value = false;
  }
</script>

<template>
  <main>
    <form @submit.prevent="handleSubmit">
      <div class="form-col-group">
        <label for="importFile">Import File:</label>
        <input type="file" name="importFile" id="importFile" accept=".csv" />
      </div>
      <div class="form-row-group">
        <input type="checkbox" name="hasHeader" id="hasHeader" checked />
        <label for="hasHeader">File has header row</label>
      </div>
      <button type="submit" :disabled="isAnalyzing === true">
        <span v-if="isAnalyzing">Analyzing...</span>
        <span v-else>Analyze</span>
      </button>
    </form>
    <div v-if="importAnalysis !== null" class="response-container">
      <p>It looks like you need to create an app like this to hold your data.</p>
      <h2>{{ importAnalysis.appName }}</h2>
      <table>
        <thead>
          <tr>
            <th>Field Name</th>
            <th>Field Type</th>
            <th>Field Description</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="column in importAnalysis.columns" :key="column.name">
            <td>{{ column.name }}</td>
            <td>{{ column.type }}</td>
            <td>{{ column.description }}</td>
          </tr>
        </tbody>
      </table>
      <p>Would you like to create this app now?</p>
      <button type="button" @click="handleCreateAppButtonClick" :disabled="isCreatingApp === true">
        <span v-if="isCreatingApp">Creating...</span>
        <span v-else>Create App</span>
      </button>
      <p v-if="createdAppUrl !== null">
        App created successfully. You can view it <a :href="createdAppUrl" target="_blank">here</a>.
      </p>
      <div v-if="createdAppUrl !== null" class="create-import-container">
        <p>Would you like to import the data now?</p>
        <button
          type="button"
          @click="handleImportDataButtonClick"
          :disabled="isImportingData === true"
        >
          <span v-if="isImportingData">Importing...</span>
          <span v-else>Import Data</span>
        </button>
        <p v-if="reportUrl !== null">
          Import successful. You can view the data in a report
          <a :href="reportUrl" target="_blank">here</a>.
        </p>
      </div>
    </div>
  </main>
</template>

<style scoped>
  main {
    display: flex;
    flex-direction: column;
    align-items: center;
    margin: 1rem;
    gap: 2rem;
    height: 100%;
  }

  form {
    display: flex;
    flex-direction: column;
    gap: 1rem;

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
    display: flex;
    flex-direction: column;
    gap: 1rem;

    & button {
      padding: 0.5rem 1rem;
      background-color: var(--primary-color);
      color: white;
      border: none;
      border-radius: 0.25rem;
      cursor: pointer;
    }

    & a {
      color: var(--primary-color);
    }

    & a:hover {
      text-decoration: underline;
    }

    & .create-import-container {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }
  }

  table {
    width: 100%;
    border-collapse: collapse;

    & th,
    & td {
      border: 1px solid var(--primary-color);
      padding: 0.5rem;
    }

    & th {
      text-align: left;
      background-color: var(--primary-color);
      color: white;
    }

    & tr:nth-child(even) {
      background-color: var(--primary-color-light);
    }
  }

  button:disabled {
    background-color: gray;
  }
</style>
