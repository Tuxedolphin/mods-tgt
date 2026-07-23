<script lang="ts">
  import { CircleX, TriangleAlert } from "@lucide/svelte";
  import { onDestroy, onMount } from "svelte";
  import type { Unsubscriber } from "svelte/store";
  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import DaysOfWeekHeader from "$lib/components/DaysOfWeekHeader.svelte";
  import ModListGroup from "$lib/components/ModListGroup.svelte";
  import SearchBar from "$lib/components/SearchBar.svelte";
  import Timeline from "$lib/components/Timeline.svelte";
  import TimetableComponent from "$lib/components/TimetableComponent.svelte";
  import {
    currentlySelectedMods,
    currentUserInformation,
    currentWorkingTimetable,
    token_information,
  } from "$lib/shared/shared.svelte";
  import { roomHub } from "$lib/stores/roomHub";
  import type {
    Profile,
    RoomInformation,
    RoomProfile,
    RoomVisibility,
    TimetableDetailedResponse,
    TimetablePostTemplate,
    TimetableResponse,
  } from "$lib/types/db_raw_types";
  import { getTimetable } from "$lib/utils/format_db_information";
  import {
    format_AY_name,
    format_semester_name,
  } from "$lib/utils/formatting_utils";
  import GenericDialog from "../../GenericDialog.svelte";
  import type { PageProps } from "./$types";
  import ShareTimetableDialog from "./ShareTimetableDialog.svelte";
  import ModsSelectionComponent from "./ModsSelectionComponent.svelte";
  import FriendsMods from "./FriendsMods.svelte";

  let is_timetable_loaded = $state(false);
  let profiles: RoomProfile[] = $state([]);
  let timetable_metadata: TimetableResponse = $state({
    academicYear: "",
    createdAt: "",
    id: "",
    metaData: [],
    name: "",
    semester: 0,
  });
  // svelte-ignore non_reactive_update
  let share_tt_dialog: HTMLDialogElement;
  let currentTimetableDisplay = $derived(
    getTimetable(
      timetable_metadata.academicYear,
      timetable_metadata.semester,
      $currentlySelectedMods,
    ),
  );

  let { params }: PageProps = $props();
  let unsubscribe_from_mods_list: Unsubscriber;
  // svelte-ignore non_reactive_update
  let user_tt: TimetableDetailedResponse | undefined = $state();

  let user_perms: RoomProfile = $derived(
    profiles.find((x) => x.userId == $currentUserInformation.userId),
  );

  let room_information: RoomInformation | undefined = $state();
  let visibility: RoomVisibility = $state("restricted");
  let error = $state("");
  onMount(async () => {
    let first_time_subscribe = true;
    let update_from_room = false;

    $currentWorkingTimetable.timetable_id = params.timetable_id;
    await roomHub.connect($token_information.a!);
    try {
      room_information = await $roomHub?.invoke(
        "CreateOrJoinRoom",
        params.timetable_id,
      );

      visibility = room_information!.visibility;

      is_timetable_loaded = false;
      timetable_metadata = room_information!.timetables[0];
      profiles = room_information!.members;

      $currentlySelectedMods = room_information!.timetables;

      $roomHub?.on(
        "ReceiveTimetableUpdate",
        (msg: TimetableDetailedResponse[]) => {
          update_from_room = true;
          // If a new timetable is created: update local userid:
          if (!user_tt) {
            user_tt = msg.find(
              (x) => x.profile.userId === $currentUserInformation.userId,
            );
            update_from_room = false;
          }

          $currentlySelectedMods = msg;
        },
      );

      $roomHub?.on("ReceiveRoomVisibilityUpdate", (msg) => {
        visibility = msg;
      });
      $roomHub?.on("ReceiveRoomMembersUpdate", (msg: RoomProfile[]) => {
        console.log(msg);
        profiles = msg;
      });

      // Find a timetable that belongs to current user:
      user_tt = room_information!.timetables.find(
        (x) => x.profile.userId === $currentUserInformation.userId,
      );

      unsubscribe_from_mods_list = currentlySelectedMods.subscribe(
        async (updated_timetable) => {
          if (first_time_subscribe) {
            first_time_subscribe = false;
            return;
          }

          if (update_from_room) {
            console.log("Hello");
            update_from_room = false;
            return;
          }

          for (const timetable of updated_timetable) {
            if (timetable.profile.userId !== user_tt?.profile.userId) continue;
            await $roomHub?.invoke("UpdateTimetable", timetable.id, {
              Name: timetable.name,
              MetaData: timetable.metaData,
            });
          }
        },
      );
      is_timetable_loaded = true;
    } catch {
      error =
        "You do not have permissions to view this document. Please ask permissions from the owner of this timetable.";
    }
  });

  onDestroy(async () => {
    if (unsubscribe_from_mods_list) {
      unsubscribe_from_mods_list();
    }
    // await $roomHub?.invoke('LeaveRoom', params.timetable_id);

    roomHub.disconnect();

    currentlySelectedMods.reset();
  });
</script>

{#if is_timetable_loaded}
  <div class="flex items-center justify-between gap-2">
    <div class="flex min-w-0 flex-col">
      <h1 class="min-w-0 truncate text-lg font-semibold">
        {timetable_metadata.name}
      </h1>
      <h2 class="min-w-0 truncate text-xs">
        {format_AY_name(timetable_metadata.academicYear)} - {format_semester_name(
          timetable_metadata.semester,
        )}
      </h2>
    </div>
    <div class="flex h-8 items-center gap-1">
      <div class="flex gap-1">
        {#each profiles as profile (profile.userId)}
          {#if profile.userId !== $currentUserInformation.userId}
            <div class="avatar avatar-placeholder">
              <div class="w-8 rounded-full bg-neutral text-neutral-content">
                <span class="text-xs">{profile.username?.charAt(0)}</span>
              </div>
            </div>
          {/if}
        {/each}
      </div>
      <button class="btn btn-accent" onclick={() => share_tt_dialog.show()}>
        Share Timetable
      </button>
      <CircleX
        class="min-w-6"
        size={32}
        onclick={() => {
          goto(resolve("/(app)/home"));
        }}
      ></CircleX>
    </div>
  </div>
  <div class="flex-col flex">
    <!-- This is the main timetable view -->
    <div class="flex-col md:flex-row flex">
      <div class="flex flex-1 md:w-[75%]">
        <Timeline></Timeline>
        <div class="flex-1 flex-col">
          <DaysOfWeekHeader
            acadYear={timetable_metadata.academicYear}
            semester={timetable_metadata.semester}
          ></DaysOfWeekHeader>
          <TimetableComponent
            timetables={currentTimetableDisplay}
            acadYear={timetable_metadata.academicYear}
            semester={timetable_metadata.semester}
          ></TimetableComponent>
        </div>
      </div>

      <!-- This is the module seleciton view (your own) -->
      <div class="md:w-[25%] px-4">
        <ModsSelectionComponent
          timetable_name={timetable_metadata.name}
          {visibility}
          acadYear={timetable_metadata.academicYear}
          semester={timetable_metadata.semester}
          timetable_id={user_tt?.id}
        ></ModsSelectionComponent>
      </div>
    </div>

    <!-- This is your friend's modules: -->
    <div>
      <FriendsMods></FriendsMods>
    </div>
  </div>
{/if}

{#if error}
  <div class="w-full min-h-full text-center flex flex-col items-center">
    <TriangleAlert />
    <p>{error}</p>
  </div>
{/if}

{#if room_information}
  <ShareTimetableDialog
    room_visibility={visibility}
    base_timetable_id={params.timetable_id}
    {profiles}
    {timetable_metadata}
    bind:share_tt_dialog
  ></ShareTimetableDialog>
{/if}
