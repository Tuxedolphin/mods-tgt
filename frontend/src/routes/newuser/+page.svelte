<script lang="ts">
	import { goto } from '$app/navigation';
	import ModTogetherHero from '$lib/components/LoginPage/ModTogetherHero.svelte';
	import { token_information } from '$lib/shared/shared.svelte';
	import { put_user_info } from '$lib/utils/db_operations';
	import { resolve } from '$app/paths';

	let nameInput = $state('');
	let loading = $state(false);
	let nameError = $state(false);
	async function put_name(username: string) {
		if (nameInput.length < 1) {
			nameError = true;
			return;
		}
		loading = true;
		const result = await put_user_info($token_information.a, username);

		if (result.isOk()) {
			goto(resolve('/home'));
		}
		loading = false;
	}
</script>

<ModTogetherHero>
	<div>What shall we call you?</div>
	<label class="validator input mt-2">
		<input type="text" required bind:value={nameInput} />
	</label>
	<p class={nameError ? '' : 'hidden'}>Must be at least 1 character long.</p>
	{#if loading}
		<button class="btn btn-disabled btn-primary">Submit</button>
	{:else}
		<button class="btn btn-primary" onclick={() => put_name(nameInput)}>Submit</button>
	{/if}
</ModTogetherHero>
