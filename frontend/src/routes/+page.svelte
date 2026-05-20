<script lang="ts">
	import DaysOfWeekHeader from './DaysOfWeekHeader.svelte';

	import Timeline from '../components/Timeline.svelte';
	import SearchBar from '../components/SearchBar.svelte';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();
</script>

<SearchBar summaries={data.data}></SearchBar>
<div class="flex">
	<Timeline></Timeline>
	<div class="flex-1 gap-0">
		<DaysOfWeekHeader></DaysOfWeekHeader>
	</div>
</div>
