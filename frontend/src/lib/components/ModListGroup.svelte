<script lang="ts">
	import type { TimetableWithMetadata } from '$lib/types/db_raw_types';
	import { onDestroy, onMount } from 'svelte';
	import ModListModInfo from './ModListModInfo.svelte';
	import { currentlySelectedMods } from '$lib/shared/shared.svelte';
	import type { Unsubscriber } from 'svelte/store';
	let current_selected_mods_unsubsriber: Unsubscriber;

	interface ModListGroupProps {
		acadYear: string;
	}

	let { acadYear }: ModListGroupProps = $props();

	let updated_mod_list: TimetableWithMetadata[] = $state([]);

	onMount(() => {
		current_selected_mods_unsubsriber = currentlySelectedMods.subscribe((new_mods) => {
			updated_mod_list = [...new_mods];
		});
	});

	onDestroy(() => {
		current_selected_mods_unsubsriber();
	});
</script>

<div class="grid grid-cols-1 gap-4 p-1 lg:grid-cols-3">
	{#each updated_mod_list as tt (tt.id)}
		<ModListModInfo timetable={tt} {acadYear}></ModListModInfo>
	{/each}
</div>
