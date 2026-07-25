<script lang="ts">
  import { users_to_hide } from "$lib/shared/shared.svelte";
  import type { RoomProfile, RoomVisibility } from "$lib/types/db_raw_types";
  import type { TimeTableDayInfo } from "$lib/types/internal";
  import { findOverlappingTimeInfo } from "$lib/utils/format_db_information";
  import { get, type Unsubscriber } from "svelte/store";
  import TimetableDayComponent from "./TimetableDayComponent.svelte";
  import { onDestroy, onMount } from "svelte";

  interface DisplayInfo {
    timetableDayDisplayInfo: TimeTableDayInfo[];
    day: number;
    semester: number;
    acadYear: string;
    height_of_one_hour_lesson: number;
    timetable_name: string;
    room_profiles: RoomProfile[];
    visibility: RoomVisibility;
  }
  const {
    timetableDayDisplayInfo,
    day,
    acadYear,
    semester,
    height_of_one_hour_lesson,
    timetable_name,
    room_profiles,
    visibility,
  }: DisplayInfo = $props();

  let users_to_hide_unsub: Unsubscriber;
  let current_users_to_hide = $state([] as string[]);
  onMount(() => {
    users_to_hide.set([]);
    users_to_hide_unsub = users_to_hide.subscribe((x) => {
      current_users_to_hide = [...x];
    });
  });

  onDestroy(() => {
    if (users_to_hide_unsub) {
      users_to_hide_unsub();
    }
  });

  const filteredInformation: TimeTableDayInfo[] = $derived(
    findOverlappingTimeInfo(timetableDayDisplayInfo, current_users_to_hide),
  );
</script>

<div class="relative col-start-{day + 1} row-start-1">
  {#key filteredInformation}
    {#each filteredInformation as timetableDayInfo}
      <TimetableDayComponent
        {room_profiles}
        {visibility}
        {timetable_name}
        timetable_id={timetableDayInfo.timetableId}
        {height_of_one_hour_lesson}
        timetable_colour={timetableDayInfo.timetableColour}
        timeTableDayInfo={timetableDayInfo}
        {acadYear}
        {semester}
        timetable_index={timetableDayInfo.innerGroupIndex}
        timetable_inner_group_length={timetableDayInfo.innerGroupLength}
      ></TimetableDayComponent>
    {/each}
  {/key}
</div>
