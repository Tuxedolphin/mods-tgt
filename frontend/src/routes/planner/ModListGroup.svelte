<script lang="ts">
	import ModListModInfo from './ModListModInfo.svelte';

	import type { TimeTable } from '../../types/mod_summaries';

	interface ModListProps {
		currentTimetableDisplay: TimeTable[];
	}
	let { currentTimetableDisplay }: ModListProps = $props();
</script>

{#key currentTimetableDisplay}
	{#each currentTimetableDisplay as tt (tt.Owner)}
		{tt.Owner}'s Mod List
		<ModListModInfo lessonData={tt.LessonData}></ModListModInfo>
	{/each}
{/key}
