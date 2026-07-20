<script lang="ts">
  let { attempt_redirect = true, children } = $props();

  import mods_tgt_logo from "$lib/assets/mods_tgt_logo.png?enhanced";
  import RedirectToHomeComponent from "../UtilityComponents/RedirectToHomeComponent.svelte";
</script>

{#if attempt_redirect}
  <RedirectToHomeComponent></RedirectToHomeComponent>
{/if}

<div class="hero min-h-screen bg-base-200">
  <div class="hero-content text-center">
    <div class="max-w-xs">
      <div class="flex items-center justify-center w-full">
        <enhanced:img
          class="w-56"
          src={mods_tgt_logo}
          alt="Mods Together Logo"
        />
      </div>

      <h1 class="text-3xl font-bold">Mods Together!</h1>
      <p class="italic">We ride together. We mods together.</p>
      <div class="divider"></div>
      {@render children()}
    </div>
  </div>
</div>
