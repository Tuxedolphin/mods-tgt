<script lang="ts">
	import TimetableDayComponent from './TimetableWeekComponent.svelte';

	import { onMount } from 'svelte';
	import { currentlySelectedMods, preferences } from '../shared/shared.svelte';
	import { getFullModInfo } from '../utils/fetch_from_cache';

	import type { Module } from '../types/modules';

	const heightOfOneHourLessonPx = 16;
	let fullModInfo: { [moduleCode: string]: Module } = $state({});
	onMount(() => {
		preferences.subscribe(async () => {
			fullModInfo = {};

			// trigger UI refresh here:
			refreshUI();
		});
		currentlySelectedMods.subscribe(async () => {
			refreshUI();
		});
	});

	async function refreshUI() {
		if (!$currentlySelectedMods[$preferences.acadYear]) return;
		if (!$currentlySelectedMods[$preferences.acadYear][$preferences.currentSemView]) return; 
		for (const mod in $currentlySelectedMods[$preferences.acadYear][$preferences.currentSemView]) {
			if (mod in fullModInfo) continue;
			const info = await getFullModInfo(mod, $preferences.acadYear);
			fullModInfo[mod] = info;
		}
	}
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
