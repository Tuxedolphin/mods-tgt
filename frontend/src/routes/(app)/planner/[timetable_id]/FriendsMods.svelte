<script lang="ts">
  import {
    currentlySelectedMods,
    currentUserInformation,
  } from "$lib/shared/shared.svelte";
  import { onMount } from "svelte";
  import ModsSelectionComponent from "./ModsSelectionComponent.svelte";
  import type { RoomProfile, RoomVisibility } from "$lib/types/db_raw_types";

  let friends_mods = $derived(
    $currentlySelectedMods.filter(
      (x) => x.profile.userId !== $currentUserInformation.userId,
    ),
  );

  interface FriendsModsProps {
    visibility: RoomVisibility;
    room_profiles: RoomProfile[];
  }
  let { room_profiles, visibility }: FriendsModsProps = $props();
</script>

{#each friends_mods as mods}
  <ModsSelectionComponent
    {room_profiles}
    user_favourite_color={mods.profile.colour!}
    is_friend={true}
    acadYear={mods.academicYear}
    semester={mods.semester}
    timetable_id={mods.id}
    timetable_name={mods.name}
    {visibility}
  ></ModsSelectionComponent>
{/each}
