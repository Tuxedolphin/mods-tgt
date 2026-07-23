<script lang="ts">
  import SearchBar from "$lib/components/SearchBar.svelte";
  import { onMount } from "svelte";
  import SelectedModuleList from "./SelectedModuleList.svelte";
  import {
    currentlySelectedMods,
    currentUserInformation,
  } from "$lib/shared/shared.svelte";
  import ImportFromNUSMods from "$lib/components/ImportFromNUSMods.svelte";
  import ImportFromNusModsButton from "$lib/components/Buttons/ImportFromNusModsButton.svelte";
  import AddFromOtherTimetablesButton from "$lib/components/Buttons/AddFromOtherTimetablesButton.svelte";
  import type {
    RoomProfile,
    RoomRole,
    RoomVisibility,
  } from "$lib/types/db_raw_types";

  interface ModsSelectionComponentProps {
    acadYear: string;
    semester: number;
    timetable_id: string | undefined;

    timetable_name: string;
    is_friend: boolean;
    user_favourite_color: string;
    room_profiles: RoomProfile[];
    visibility: RoomVisibility;
  }

  let {
    acadYear,
    semester,
    timetable_id,
    visibility,
    timetable_name,
    user_favourite_color,
    room_profiles,
    is_friend = false,
  }: ModsSelectionComponentProps = $props();

  let user_info = $derived(
    $currentlySelectedMods.find((x) => x.id === timetable_id),
  );
  let selected_user_mods_list = $derived(user_info?.metaData || []);

  let user_current_perms: "annon" | RoomRole = $derived(
    room_profiles.find((x) => x.userId === $currentUserInformation.userId)
      ?.role || "annon",
  );
</script>

<!-- Logic to Show Searchbar: -->
<!-- First Layer: Check for tt metadata-->
{#if is_friend}
  @{user_info?.profile.handle}'s Mod List:
{:else}
  Your Mod List:
{/if}

{#if visibility === "publicEdit" || user_current_perms === "owner" || user_current_perms === "editor"}
  <SearchBar
    {user_favourite_color}
    {timetable_name}
    {acadYear}
    {semester}
    {timetable_id}
  ></SearchBar>
{/if}

{#if selected_user_mods_list.length !== 0}
  <SelectedModuleList
    {visibility}
    {room_profiles}
    {timetable_name}
    {acadYear}
    {semester}
    {timetable_id}
  ></SelectedModuleList>
{:else}
  <div class="text-center">List is empty.</div>
  {#if !is_friend}
    {#if visibility === "publicEdit" || user_current_perms === "owner" || user_current_perms === "editor"}
      <div class="flex text-center flex-col">
        <ImportFromNusModsButton
          user_favourite_colour={user_favourite_color}
          acad_year={acadYear}
          {semester}
          current_timetable_id={timetable_id}
          timetable_name={$currentlySelectedMods[0].name}
        ></ImportFromNusModsButton>
      </div>
      <div class="divider">OR</div>
      <div class="w-full">
        <AddFromOtherTimetablesButton
          acad_year={acadYear}
          current_timetable_id={timetable_id}
          timetable_name={$currentlySelectedMods[0].name}
          {semester}
        ></AddFromOtherTimetablesButton>
      </div>
    {/if}
  {/if}
{/if}
<!-- 
{#if selected_user_mods_list.length !== 0}
  <SelectedModuleList {timetable_name} {acadYear} {semester} {timetable_id}
  ></SelectedModuleList>
{:else}
  <div>List is empty.</div>
  {#if user_current_perms === "editor" || user_current_perms === "owner"}
    {#if !is_friend}
      <div class="flex text-center flex-col">
        <ImportFromNusModsButton
          user_favourite_colour={user_favourite_color}
          acad_year={acadYear}
          {semester}
          current_timetable_id={timetable_id}
          timetable_name={$currentlySelectedMods[0].name}
        ></ImportFromNusModsButton>
      </div>
      <div class="divider">OR</div>
      <div class="w-full">
        <AddFromOtherTimetablesButton
          acad_year={acadYear}
          current_timetable_id={timetable_id}
          timetable_name={$currentlySelectedMods[0].name}
          {semester}
        ></AddFromOtherTimetablesButton>
      </div>
    {/if}
  {:else if user_current_perms === "annon" && visibility === "publicEdit"}
    <div class="flex text-center flex-col">
      <ImportFromNusModsButton
        user_favourite_colour={user_favourite_color}
        acad_year={acadYear}
        {semester}
        current_timetable_id={timetable_id}
        timetable_name={$currentlySelectedMods[0].name}
      ></ImportFromNusModsButton>
    </div>
    <div class="divider">OR</div>
    <div class="w-full">
      <AddFromOtherTimetablesButton
        acad_year={acadYear}
        current_timetable_id={timetable_id}
        timetable_name={$currentlySelectedMods[0].name}
        {semester}
      ></AddFromOtherTimetablesButton>
    </div>
  {:else if user_current_perms === "annon"}
    <div>You do not have permissions modify.</div>
  {/if}
{/if} -->
