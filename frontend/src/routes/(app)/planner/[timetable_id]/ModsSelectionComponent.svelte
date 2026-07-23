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

  interface ModsSelectionComponentProps {
    acadYear: string;
    semester: number;
    timetable_id: string | null;
  }

  let { acadYear, semester, timetable_id }: ModsSelectionComponentProps =
    $props();

  let user_info = $derived(
    $currentlySelectedMods.find((x) => x.id === timetable_id),
  );
  let selected_user_mods_list = $derived(user_info?.metaData || []);
</script>

{#if user_info}
  <div class="text-xl">
    {#if user_info.profile.userId === $currentUserInformation.userId}
      Your Mods:
    {:else}
      {user_info.profile.handle}
    {/if}
  </div>
  <SearchBar {acadYear} {semester} {timetable_id}></SearchBar>
{/if}

{#if selected_user_mods_list.length !== 0}
  <SelectedModuleList {acadYear} {semester} {timetable_id}></SelectedModuleList>
{:else}
  <div class="flex text-center flex-col">
    <div>List is empty.</div>
    <ImportFromNusModsButton
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
      {semester}
    ></AddFromOtherTimetablesButton>
  </div>
{/if}
