<script lang="ts">
	import { currentlySelectedMods } from '../shared/shared.svelte';
	import type { ModSummary } from '../types/mod_summaries';
	import { get } from 'svelte/store';

	interface Mod {
		mod: ModSummary;
	}

	let { mod }: Mod = $props();

	function addMod() {
		currentlySelectedMods.set({
			selectedMods: mod.moduleCode
		});
	}
</script>

<button class="h-12 w-full" onclick={addMod}>{mod.moduleCode} - {mod.title}</button>
