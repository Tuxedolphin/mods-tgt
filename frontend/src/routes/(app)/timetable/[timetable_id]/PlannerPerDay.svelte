<script lang="ts">
  import { currentUserInformation } from "$lib/shared/shared.svelte";
  import type { RoomInformation, TimetableModule } from "$lib/types/db_raw_types";
  import { getFullModInfo } from "$lib/utils/fetch_from_cache";
  import { onMount } from "svelte";

    interface PlannerPerDayProps {
        day_number: number,
        room_information: RoomInformation
        acad_year: string,
        sem_number: number
    }
    let { day_number, room_information, acad_year, sem_number }: PlannerPerDayProps = $props();

    let schedule = $derived(room_information.timetables.toSorted(x => x.profile.handle == $currentUserInformation.handle ? -1 : 1))

    onMount(() => {
        // console.log($inspect(schedule))
    })

    async function resolve_time_module(tt_info: TimetableModule[]) {
        for (let i = 0; i < tt_info.length; i++) {
            const element = tt_info[i];
            const info = await getFullModInfo(element.moduleCode, acad_year)
            const sem_data = info.semesterData.filter(x => x.semester === sem_number);
            console.log(sem_data)
        }
    }
</script>

{#each schedule as timetable}
    {#await resolve_time_module(timetable.metaData)}
        <div>{timetable.metaData}</div>
    {:then result} 
        <div>{result}</div>
    {/await}
    
{/each}