<script lang="ts">
	import { currentlySelectedMods, preferences } from '../shared/shared.svelte';
	import type { ModSummary } from '../types/mod_summaries';
	import { getFullModInfo } from '../utils/fetch_from_cache';

	interface Mod {
		mod: ModSummary;
	}

	let { mod }: Mod = $props();

	async function addMod() {
		if (!mod.semesters.includes($preferences.currentSemView)) return;
		const modFullInfo = await getFullModInfo(mod.moduleCode, $preferences.acadYear);

		const timeTable = modFullInfo.semesterData.find(
			(sem) => sem.semester == $preferences.currentSemView
		)?.timetable;
		// Source - https://stackoverflow.com/a/75356213
		// Posted by Peter Thoeny
		// Retrieved 2026-05-21, License - CC BY-SA 4.0
		const group = timeTable?.reduce((acc, obj) => {
			if (!acc[obj.lessonType]) acc[obj.lessonType] = {};
			if (!acc[obj.lessonType][obj.classNo]) acc[obj.lessonType][obj.classNo] = [];
			acc[obj.lessonType][obj.classNo].push(obj);

			return acc;
		}, {});

		let pairings = {};
		for (const key in group) {
			const firstLesson = Object.keys(group[key])[0];
			pairings[key] = firstLesson;
		}

		if (!$currentlySelectedMods[$preferences.acadYear]) {
			$currentlySelectedMods[$preferences.acadYear] = {};
		}

		if (!$currentlySelectedMods[$preferences.acadYear][$preferences.currentSemView]) {
			$currentlySelectedMods[$preferences.acadYear][$preferences.currentSemView] = {};
		}
		$currentlySelectedMods[$preferences.acadYear][$preferences.currentSemView][
			modFullInfo.moduleCode
		] = pairings;

		
	}
</script>

<button class="h-12 w-full" onclick={addMod}> 
	{#if !mod.semesters.includes($preferences.currentSemView)}
		{mod.moduleCode} - {mod.title} - Not Offered in this semester
	{:else}
		{mod.moduleCode} - {mod.title}
	{/if}
</button>
