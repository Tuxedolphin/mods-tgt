<script lang="ts">
  import DayDisplay from "./DayDisplay.svelte";

  import { onMount } from "svelte";

  import { roomHub } from "$lib/stores/roomHub";
  import { token_information } from "$lib/shared/shared.svelte";
  import type { PageProps } from "./$types";
  import type { RoomInformation } from "$lib/types/db_raw_types";
  import PlannerPerDay from "./PlannerPerDay.svelte";

  let { params }: PageProps = $props();
  let room_information: RoomInformation | undefined = $state();
  let acad_year: string = $state("");
  let sem_number: number = $state(0);
  onMount(async () => {
    await roomHub.connect($token_information.a!);

    room_information = await $roomHub?.invoke(
      "CreateOrJoinRoom",
      params.timetable_id,
    );

    acad_year = room_information?.timetables[0].academicYear!;
    sem_number = room_information?.timetables[0].semester!;
  });

  let length_of_one_hour = "min-w-32";
  let day_height = "min-h-16";
  let day_width = "w-16";
  let days = ["Mon", "Tue", "Wed", "Thu", "Fri"];
</script>

<div>
  <div class="flex">
    <DayDisplay></DayDisplay>
    <div class="flex flex-col overflow-auto">
      <div class="flex h-6">
        {#each { length: 14 }, i}
          <div class="{length_of_one_hour} border">{i}</div>
        {/each}
      </div>
      {#if room_information}
        {#each { length: 5 }, i}
          <PlannerPerDay {sem_number} {acad_year} {room_information} day_number={i}></PlannerPerDay>
        {/each}
      {/if}
    </div>
  </div>
</div>
