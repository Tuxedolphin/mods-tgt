<script lang="ts">
  import { onDestroy, onMount } from "svelte";
  import type { Unsubscriber } from "svelte/store";
  import {
    timetable_list_should_be_refreshed,
    token_information,
  } from "$lib/shared/shared.svelte";
  import type { TimetableInfos } from "$lib/types/db_raw_types";
  import { get_timetables } from "$lib/utils/db_operations";
  import TimeTableCardComponent from "./TimeTableCardComponent.svelte";

  let availableTimetables: TimetableInfos = $state([]);
  let unsubscribe_from_refresh: Unsubscriber;
  onMount(async () => {
    unsubscribe_from_refresh = timetable_list_should_be_refreshed.subscribe(
      async (should_be_refreshed) => {
        if (!should_be_refreshed) return;
        const timetable_request = await get_timetables($token_information.a);
        if (timetable_request.isOk()) {
          availableTimetables = [...timetable_request.value];
          console.log("Refresh Tt");
        }

        timetable_list_should_be_refreshed.set(false);
      },
    );

    timetable_list_should_be_refreshed.set(true);
  });

  onDestroy(() => {
    unsubscribe_from_refresh();
  });
</script>

<div class="grid grid-cols-1 gap-4 lg:grid-cols-3">
  {#each availableTimetables as timetable (timetable.id)}
    <TimeTableCardComponent access_token={$token_information.a} {timetable}
    ></TimeTableCardComponent>
  {/each}
</div>
