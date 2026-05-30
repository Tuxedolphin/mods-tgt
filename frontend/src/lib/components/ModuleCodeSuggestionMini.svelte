<script lang="ts">
	import {
		currentlySelectedMods,
		currentUserInformation,
		preferences,
		searchTerm
	} from '$lib/shared/shared.svelte';
	import type { ModSummary } from '$lib/types/mod_summaries';
	import { getFullModInfo } from '$lib/utils/fetch_from_cache';
	import { checkModAlreadyAdded, createModEntry } from '$lib/utils/format_db_information';

	interface ModSuggestionsProp {
		mod: ModSummary;
		semester: number;
		acadYear: string;
	}

	let { mod, semester, acadYear }: ModSuggestionsProp = $props();

	async function addMod() {
		if (!mod.semesters.includes(semester)) return;
		const modFullInfo = await getFullModInfo(mod.moduleCode, acadYear);

		const timeTable = modFullInfo.semesterData.find((sem) => sem.semester == semester)!.timetable;

		if (
			checkModAlreadyAdded(
				$currentlySelectedMods,
				acadYear,
				semester,
				$currentUserInformation.displayName,
				'test',
				mod.moduleCode
			)
		)
			return;
		currentlySelectedMods.set(
			await createModEntry(
				$currentlySelectedMods,
				acadYear,
				semester,
				$currentUserInformation.displayName,
				'test',
				modFullInfo.moduleCode,
				timeTable
			)
		);

		searchTerm.set('');
	}
</script>

<button class="h-12 w-full" onclick={addMod}>
	{#if !mod.semesters.includes($preferences.currentSemView)}
		{mod.moduleCode} - {mod.title} - Not Offered in this semester
	{:else}
		{mod.moduleCode} - {mod.title}
	{/if}
</button>
