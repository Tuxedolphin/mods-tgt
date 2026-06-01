<script lang="ts">
	import CreateNewTimetableButton from './CreateNewTimetableButton.svelte';

	import { onMount } from 'svelte';
	import GreetingComponent from '$lib/components/GreetingComponent.svelte';
	import { access_token } from '$lib/shared/shared.svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import AvailableTimetableGrid from '$lib/components/AvailableTimetableGrid.svelte';

	let token = $state('');
	onMount(() => {
		if (!$access_token.access_token) {
			goto(resolve('/login'));
		}

		token = $access_token.access_token;
	});
</script>

{#if token}
	<div class="flex justify-between py-4">
		<GreetingComponent access_token={token}></GreetingComponent>
		<div class="flex gap-2">
			<CreateNewTimetableButton></CreateNewTimetableButton>
			<button
				class="btn btn-error"
				onclick={() => {
					access_token.reset();
					const message = 'Logout Successful';
					goto(resolve(`/login#error_description=${message}`));
				}}>Logout</button
			>
		</div>
	</div>

	<AvailableTimetableGrid access_token={token}></AvailableTimetableGrid>
{/if}
