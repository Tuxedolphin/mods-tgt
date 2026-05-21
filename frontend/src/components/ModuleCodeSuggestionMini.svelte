<script lang="ts">
	import { currentlySelectedMods } from '../shared/shared.svelte';
	import type { ModSummary } from '../types/mod_summaries';
	import type { Module } from '../types/modules';

	interface Mod {
		mod: ModSummary;
	}

	let { mod }: Mod = $props();

	async function addMod() {
		const modFullInfo = (await (
			await fetch(`https://api.nusmods.com/v2/2025-2026/modules/${mod.moduleCode}.json`)
		).json()) as Module;

		const timeTable = modFullInfo.semesterData.find((sem) => sem.semester == 2)?.timetable;
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
		$currentlySelectedMods.selectedMods[mod.moduleCode] = pairings;
	}
</script>

<button class="h-12 w-full" onclick={addMod}>{mod.moduleCode} - {mod.title}</button>
