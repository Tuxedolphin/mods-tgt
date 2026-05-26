<script lang="ts">
	import type { LessonData } from '../../types/mod_summaries';

	interface ModListModInfoProps {
		lessonData: LessonData[];
	}
	let { lessonData }: ModListModInfoProps = $props();
	const lessons = $derived(Object.keys(Object.groupBy(lessonData, (x) => x.ModuleCode)));
</script>

{#each lessons as lesson (lesson)}
	<div>{lesson}</div>
{/each}
