<script lang="ts">
  import UserAvatarComponent from "$lib/components/Profile/UserAvatarComponent.svelte";
  import { currentUserInformation } from "$lib/shared/shared.svelte";
  import type { RoomProfile } from "$lib/types/db_raw_types";

  interface CurrentlyViewingDocumentComponentProps {
    profiles: RoomProfile[];
  }

  let { profiles }: CurrentlyViewingDocumentComponentProps = $props();

  let currently_online = $derived(
    profiles.filter(
      (x) => x.isInRoom && x.userId !== $currentUserInformation.userId,
    ),
  );

  let max_users_shown = 2;
</script>

<div class="flex join">
  {#each currently_online as profile, i}
    {#if i < max_users_shown}
      {#if profile.userId !== $currentUserInformation.userId && profile.isInRoom}
        <UserAvatarComponent user_info={profile}></UserAvatarComponent>
      {/if}
    {:else}
      <UserAvatarComponent
        raw_text="+{currently_online.length - max_users_shown}"
      ></UserAvatarComponent>
    {/if}
  {/each}
</div>
