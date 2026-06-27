<script lang="ts">
	import ModInfoCard from './ModInfoCard.svelte';

	import type { TimetableDetailedResponse } from '$lib/types/db_raw_types';
	interface ModListModInfoProps {
		timetable: TimetableDetailedResponse;
		acadYear: string;
	}
	let { timetable, acadYear }: ModListModInfoProps = $props();
	let lesson_groups = $derived(Object.groupBy(timetable.metaData, (x) => x.moduleCode));
	let lesson_headers = $derived(Object.keys(lesson_groups));
</script>

{#each lesson_headers as lesson}
	<ModInfoCard {lesson_groups} lesson_header={lesson} {acadYear} {timetable}></ModInfoCard>
{/each}
