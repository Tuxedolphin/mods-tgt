<script>
  import { currentUserInformation } from "$lib/shared/shared.svelte";

  let { size = 32 } = $props();
</script>

<div class="avatar avatar-placeholder">
  <div
    class="rounded-full bg-neutral text-neutral-content"
    style="width: {size}px;"
  >
    {#if $currentUserInformation.avatarUrl}
      <img src={$currentUserInformation.avatarUrl} />
    {:else}
      <span class="text-3xl">{$currentUserInformation.username?.charAt(0)}</span
      >
    {/if}
  </div>
</div>
