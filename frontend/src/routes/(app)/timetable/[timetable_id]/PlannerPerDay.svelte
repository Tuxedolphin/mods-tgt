<script lang="ts">
    import { currentUserInformation } from "$lib/shared/shared.svelte";
    import type {
        RoomInformation,
        TimetableDetailedResponse,
        TimetableModule,
    } from "$lib/types/db_raw_types";
    import type { RawLesson } from "$lib/types/modules";
    import { getFullModInfo } from "$lib/utils/fetch_from_cache";
    import { daysOfWeek } from "$lib/utils/format_db_information";
    import type { FullLessonInformation, TimetableFullInformation } from "$lib/utils/frontend_formatted_types";
    import { onMount } from "svelte";

    interface PlannerPerDayProps {
        day_number: number;
        room_information: RoomInformation;
        acad_year: string;
        sem_number: number;
    }
    let {
        day_number,
        room_information,
        acad_year,
        sem_number,
    }: PlannerPerDayProps = $props();

    let schedule = $derived(
        room_information.timetables.toSorted((x) =>
            x.profile.handle == $currentUserInformation.handle ? -1 : 1,
        ),
    );

    onMount(() => {
    });

    async function resolve_time_module(tt_info: TimetableDetailedResponse): Promise<TimetableFullInformation> {
        const metadata = tt_info.metaData;
        const day_string = daysOfWeek[day_number];

        let final_lesson_info: FullLessonInformation[] = [];
        for (let i = 0; i < metadata.length; i++) {
            const element = metadata[i];
            const info = await getFullModInfo(element.moduleCode, acad_year);
            const sem_data = info.semesterData.filter(
                (x) => x.semester === sem_number,
            )[0];
            const final = sem_data.timetable.filter(
                (x) =>
                    x.classNo == element.lessonNo &&
                    x.lessonType == element.lessonType &&
                    x.day === day_string,
            ) as FullLessonInformation[];
            for (let j = 0; j < final.length; j++) {
              final[j].module = info;
            }
            final_lesson_info.push(... final);
        }

        return { ...tt_info, rawLesson: final_lesson_info };
    }
</script>

{#each schedule as timetable}
    {#await resolve_time_module(timetable)}
        <div>Loading TT</div>
    {:then result}
        <div>{day_number}, {result.profile.username}</div>
        {#each result.rawLesson as lesson}
            <div>{lesson.classNo}, {lesson.lessonType}, {lesson.module.moduleCode}, {lesson.startTime}, {lesson.endTime}</div>
        {/each}
    {/await}
{/each}
