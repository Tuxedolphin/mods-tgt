<script lang="ts">
	import CreateNewTimetableButton from './CreateNewTimetableButton.svelte';

	import { onMount } from 'svelte';
	import GreetingComponent from '$lib/components/GreetingComponent.svelte';
	import { token_information } from '$lib/shared/shared.svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import AvailableTimetableGrid from '$lib/components/AvailableTimetableGrid.svelte';

	let token = $state('');
	onMount(() => {
		if (!$token_information.a) {
			goto(resolve('/login'));
		}

		token = $token_information.a;
	});
</script>

{#if token}
	<div class="mx-0.5 flex items-center justify-between pt-2 pb-4">
		<GreetingComponent access_token={token}></GreetingComponent>
		<div class="flex gap-2">
			<CreateNewTimetableButton></CreateNewTimetableButton>
		</div>
	</div>

	<AvailableTimetableGrid access_token={token}></AvailableTimetableGrid>
{/if}
