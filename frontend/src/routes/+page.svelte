<script lang="ts">
	import TimetableComponent from '../components/TimetableComponent.svelte';

	import DaysOfWeekHeader from '../components/DaysOfWeekHeader.svelte';

	import Timeline from '../components/Timeline.svelte';
	import SearchBar from '../components/SearchBar.svelte';
	import type { PageProps } from './$types';
	import { currentlySelectedMods } from '../shared/shared.svelte';

	let { data }: PageProps = $props();
</script>

<SearchBar summaries={data.data}></SearchBar>
{$currentlySelectedMods.selectedMods}
<div class="flex">
	<Timeline></Timeline>
	<div class="flex-1 flex-col">
		<DaysOfWeekHeader></DaysOfWeekHeader>
		<TimetableComponent></TimetableComponent>
	</div>
</div>
