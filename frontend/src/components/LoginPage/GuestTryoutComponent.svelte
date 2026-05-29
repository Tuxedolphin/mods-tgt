<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';
	import { currentUserInformation } from '../../shared/shared.svelte';

	let nameInput = $state('');
	onMount(() => {
		if ($currentUserInformation.displayName) {
			goto(resolve('/planner'));
		}
	});
</script>

<div>Try it out as a guest:</div>
<div class="py-4">
	<input type="text" placeholder="Enter your name" class="input" bind:value={nameInput} />
</div>

<button
	class="btn {nameInput.length === 0 ? 'btn-disabled' : ''} btn-primary"
	onclick={() => {
		$currentUserInformation.displayName = nameInput;
		$currentUserInformation.isGuest = true;
		goto(resolve('/planner'));
	}}>Plan as a guest</button
>
