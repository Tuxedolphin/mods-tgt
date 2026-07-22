<script>
  import { currentUserInformation } from "$lib/shared/shared.svelte";

  let { size = 32, user_info = $currentUserInformation } = $props();
</script>

<div class="avatar avatar-placeholder">
  <div
    class="rounded-full bg-neutral text-neutral-content"
    style="width: {size}px;"
  >
    {#if user_info.avatarUrl}
      <img alt="User Avatar" src={user_info.avatarUrl} />
    {:else}
      <span class="text-3xl">{user_info.username?.charAt(0)}</span>
    {/if}
  </div>
</div>
