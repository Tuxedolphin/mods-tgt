<script lang="ts">
	import type { TimetableLessonMetadata } from '$lib/types/db_raw_types';

	interface ModListModInfoProps {
		lessonData: TimetableLessonMetadata[];
	}
	let { lessonData }: ModListModInfoProps = $props();
	const lessons = $derived(Object.keys(Object.groupBy(lessonData, (x) => x.moduleCode)));
</script>

{#each lessons as lesson (lesson)}
	<div>{lesson}</div>
{/each}
