<script lang="ts">
  import {
    chooseModState,
    currentlySelectedMods,
    currentUserInformation,
  } from "$lib/shared/shared.svelte";
  import { roomHub } from "$lib/stores/roomHub";
  import type {
    RoomProfile,
    RoomRole,
    RoomVisibility,
  } from "$lib/types/db_raw_types";
  import type { TimeTableDayInfo } from "$lib/types/internal";
  import { modifyModEntry } from "$lib/utils/format_db_information";
  import { get } from "svelte/store";

  interface TimetableDayProps {
    timeTableDayInfo: TimeTableDayInfo;
    semester: number;
    acadYear: string;
    timetable_id: string;
    timetable_colour: string;
    height_of_one_hour_lesson: number;
    timetable_name: string;
    room_profiles: RoomProfile[];
    visibility: RoomVisibility;
  }
  const {
    timeTableDayInfo,
    acadYear,
    semester,
    timetable_id,
    timetable_colour,
    height_of_one_hour_lesson,
    timetable_name,
    room_profiles,
    visibility,
  }: TimetableDayProps = $props();

  let user_current_perms: "annon" | RoomRole = $derived(
    room_profiles.find((x) => x.userId === $currentUserInformation.userId)
      ?.role || "annon",
  );

  const spaceAllowedToUse = $derived(100.0 / timeTableDayInfo.outerGroupLength);
  const startingOuterOffset = $derived(
    timeTableDayInfo.outerGroupIndex * spaceAllowedToUse,
  );
  const leftMarginPercentage = $derived(
    timeTableDayInfo.innerGroupIndex *
      (spaceAllowedToUse / timeTableDayInfo.innerGroupLength) +
      startingOuterOffset,
  );
  const showModName = $state(false);
  const width = $derived(spaceAllowedToUse / timeTableDayInfo.innerGroupLength);

  async function changeTimetable() {
    if (
      visibility === "publicEdit" ||
      user_current_perms === "editor" ||
      user_current_perms === "owner"
    ) {
      if ($chooseModState.lessonType === "") {
        $chooseModState = {
          lessonType: timeTableDayInfo.lessonSchedule.lessonType,
          moduleCode: timeTableDayInfo.moduleCode,
          classNo: timeTableDayInfo.lessonSchedule.classNo,
          colour: timetable_colour,
          selectedTimetableId: timetable_id,
        };
      } else {
        currentlySelectedMods.set(
          await modifyModEntry(
            $currentlySelectedMods,
            acadYear,
            semester,
            timetable_id,
            timeTableDayInfo.moduleCode,
            timeTableDayInfo.lessonSchedule.lessonType,
            timeTableDayInfo.lessonSchedule.classNo,
            $chooseModState,
          ),
        );

        const new_data = get(currentlySelectedMods).find(
          (x) => x.id === timetable_id,
        )!.metaData;

        await $roomHub?.invoke("UpdateTimetable", timetable_id, {
          Name: timetable_name,
          MetaData: new_data,
        });

        $chooseModState = {
          classNo: "",
          colour: "",
          lessonType: "",
          moduleCode: "",
          selectedTimetableId: "",
        };
      }
    }
  }

  function styledAsPossibleSelection(): string {
    return timeTableDayInfo.isAChoiceSelection ? "opacity-30" : "opacity-100";
  }

  // svelte-ignore state_referenced_locally
  const pixel_conversion = 12 * height_of_one_hour_lesson;

  function calculateHeight(): string {
    const rounded_height = Math.round(
      timeTableDayInfo.normalisedEndDuration * pixel_conversion -
        timeTableDayInfo.normalisedStartDuration * pixel_conversion,
    );
    return `h-${rounded_height}`;
  }
  function calculateTopMargin(): string {
    const rounded_top_margin = Math.round(
      timeTableDayInfo.normalisedStartDuration * pixel_conversion,
    );
    return `mt-${rounded_top_margin}`;
  }
</script>

<!-- svelte-ignore a11y_click_events_have_key_events -->
<!-- svelte-ignore a11y_no_static_element_interactions -->
<div
  style:margin-left="{leftMarginPercentage}%"
  style:width="{width}%;"
  class="absolute
	rounded
	p-1
	{calculateHeight()}
	{styledAsPossibleSelection()}
	{calculateTopMargin()}  
	{timetable_colour} 
	text-[10px]
	wrap-break-word
	text-black
	md:text-xs"
  onclick={async () => changeTimetable()}
>
  <div class="font-semibold">
    {timeTableDayInfo.moduleCode}
    {showModName ? timeTableDayInfo.moduleName : ""}
  </div>

  <div class="flex gap-1">
    <div class="truncate">
      {timeTableDayInfo.lessonSchedule.lessonType}
    </div>
    <div class="opacity-50">
      [{timeTableDayInfo.lessonSchedule.classNo}]
    </div>
  </div>

  <div class="text-[10px] italic">
    {#if timeTableDayInfo.timetableOwner?.userId === $currentUserInformation.userId}
      {timeTableDayInfo.timetableOwner?.username} (You)
    {:else}
      {timeTableDayInfo.timetableOwner?.username}
    {/if}
  </div>
</div>
