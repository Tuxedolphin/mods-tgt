<script>
  import { currentUserInformation } from "$lib/shared/shared.svelte";

  let {
    size = 32,
    user_info = $currentUserInformation,
    raw_text = "",
  } = $props();
</script>

<div class="avatar avatar-placeholder">
  <div
    class="@container rounded-full bg-neutral text-neutral-content"
    style="width: {size}px;"
  >
    {#if raw_text}
      <span class="text-[60cqi]">{raw_text}</span>
    {:else if user_info.avatarUrl}
      <img alt="User Avatar" src={user_info.avatarUrl} />
    {:else}
      <span class="text-[60cqi]">{user_info.username?.charAt(0)}</span>
    {/if}
  </div>
</div>
