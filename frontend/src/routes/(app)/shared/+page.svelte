<script lang="ts">
  import { onDestroy, onMount } from "svelte";
  import type { Unsubscriber } from "svelte/store";
  import {
    timetable_list_should_be_refreshed,
    token_information,
  } from "$lib/shared/shared.svelte";
  import { get_shared_timetables } from "$lib/utils/db_operations";

  import mods_tgt_peek from "$lib/assets/mods_tgt_peek.png?enhanced";
  import TimetableList from "./TimetableList.svelte";
  import type { SharedTimetableSummaryResponse } from "$lib/types/db_raw_types";
  let availableTimetables: SharedTimetableSummaryResponse[] = $state([]);
  let loading = $state(false);
  let unsubscribe_from_refresh: Unsubscriber;
  onMount(async () => {
    unsubscribe_from_refresh = timetable_list_should_be_refreshed.subscribe(
      async (should_be_refreshed) => {
        if (!should_be_refreshed) return;
        loading = true;
        const timetable_request = await get_shared_timetables(
          $token_information.a,
        );
        if (timetable_request.isOk()) {
          console.log(timetable_request.value);
          availableTimetables = [...timetable_request.value];
          loading = false;
        }

        timetable_list_should_be_refreshed.set(false);
      },
    );

    timetable_list_should_be_refreshed.set(true);
  });

  onDestroy(() => {
    if (unsubscribe_from_refresh) {
      unsubscribe_from_refresh();
    }
  });
</script>

<p class="mt-2 text-xl font-semibold">Shared with me</p>
<hr class="my-2 h-px border-0 bg-gray-200" />
{#if !loading}
  {#if availableTimetables.length === 0}
    <div class="flex items-center flex-col">
      <enhanced:img class="w-64" src={mods_tgt_peek} alt="Mods Together Logo" />
      <div>Timetables shared with you will appear here!</div>
    </div>
  {:else}
    <TimetableList
      {availableTimetables}
      editing_allowed={false}
      deletion_allowed={false}
    ></TimetableList>
  {/if}
{/if}
