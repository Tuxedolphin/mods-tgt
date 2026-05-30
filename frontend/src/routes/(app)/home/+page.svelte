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
	<div class="flex justify-between">
		<GreetingComponent access_token={token}></GreetingComponent>
		<CreateNewTimetableButton></CreateNewTimetableButton>
	</div>

	<AvailableTimetableGrid access_token={token}></AvailableTimetableGrid>
{/if}
