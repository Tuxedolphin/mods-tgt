<script lang="ts">
  import {
    currentlySelectedMods,
    currentUserInformation,
  } from "$lib/shared/shared.svelte";
  import { onMount } from "svelte";

  let friends_mods = $derived(
    $currentlySelectedMods.find(
      (x) => x.profile.userId !== $currentUserInformation.userId,
    ),
  );

  onMount(() => {
    console.log(friends_mods);
  });
</script>

<!-- {#each friends_mods as mods}{/each} -->
