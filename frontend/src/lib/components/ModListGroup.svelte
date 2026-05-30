<script lang="ts">
	import type { TimetableWithMetadata } from '$lib/types/db_raw_types';
	import ModListModInfo from './ModListModInfo.svelte';

	interface ModListProps {
		currentTimetableDisplay: TimetableWithMetadata[];
	}
	let { currentTimetableDisplay }: ModListProps = $props();
</script>

{#key currentTimetableDisplay}
	{#each currentTimetableDisplay as tt (tt.id)}
		<ModListModInfo lessonData={tt.metaData}></ModListModInfo>
	{/each}
{/key}
