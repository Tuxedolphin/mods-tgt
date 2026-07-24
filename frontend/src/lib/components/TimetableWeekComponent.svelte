<script lang="ts">
  import type { RoomProfile, RoomVisibility } from "$lib/types/db_raw_types";
  import type { TimeTableDayInfo } from "$lib/types/internal";
  import { findOverlappingTimeInfo } from "$lib/utils/format_db_information";
  import TimetableDayComponent from "./TimetableDayComponent.svelte";

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

  const filteredInformation: TimeTableDayInfo[] = $derived(
    findOverlappingTimeInfo(timetableDayDisplayInfo),
  );
</script>

<div class="relative col-start-{day + 1} row-start-1">
  {#each filteredInformation as timetableDayInfo (timetableDayInfo)}
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
    ></TimetableDayComponent>
  {/each}
</div>
