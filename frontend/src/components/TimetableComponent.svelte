<script lang="ts">
	import TimetableDayComponent from './TimetableDayComponent.svelte';

	import { onMount } from 'svelte';
	import { currentlySelectedMods } from '../shared/shared.svelte';
	import { getFullModInfo } from '../utils/fetchFromCache';

	import type { Module } from '../types/modules';

	const heightOfOneHourLessonPx = 16;
	let fullModInfo: { [moduleCode: string]: Module } = $state({});
	onMount(() => {
		currentlySelectedMods.subscribe(async (mods) => {
			for (const mod in mods.selectedMods) {
				if (mod in fullModInfo) continue;
				const info = await getFullModInfo(mod);
				fullModInfo[mod] = info;
			}
		});
	});
</script>

<div class="grid grid-cols-5 grid-rows-12">
	{#each { length: 12 }, y}
		{#each { length: 5 }, x}
			<div
				class="col-start-{x + 1} row-start-{y + 1} h-{heightOfOneHourLessonPx} {y % 2 == 0
					? 'bg-base-300'
					: 'bg-base-200'}"
			></div>
		{/each}
	{/each}
	{#each { length: 5 }, day}
		<TimetableDayComponent {day} modInfo={fullModInfo}></TimetableDayComponent>
	{/each}
</div>
