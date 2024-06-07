<script setup lang="ts">
  import { RouterLink } from 'vue-router';
</script>

<template>
  <main>
    <nav>
      <ul>
        <li>
          <RouterLink to="/ai-editor">AI-Enhanced Editor</RouterLink>
        </li>
      </ul>
    </nav>
  </main>
</template>

<style scoped>
  main {
    display: flex;
    justify-content: center;
    align-items: center;
    height: 100%;
  }

  nav {
    font-size: 1.5rem;
  }

  ul {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }
</style>
