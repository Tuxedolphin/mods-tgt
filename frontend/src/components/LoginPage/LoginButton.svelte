<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';
	import { access_token, currentUserInformation } from '../../shared/shared.svelte';
	import { login_to_db } from '../../utils/db_operations';

	interface LoginButtonProps {
		email: string;
		password: string;
	}
	let loading = $state(false);
	let { email, password }: LoginButtonProps = $props();
	let errorMessage = $state('');
	onMount(() => {
		if ($currentUserInformation.displayName) {
			goto(resolve('/planner'));
		}
	});

	async function login() {
		loading = true;
		const result = await login_to_db(email, password);
		if (result.isOk()) {
			// Stores access token in localstorage (FOR NOW) -- Not secure:!
			$access_token = result.value.access_token;
			goto(resolve('/planner'));
		} else {
			errorMessage = result.error;
		}
		loading = false;
	}
</script>

{#if loading}
	<button class="disabled btn mt-4 btn-neutral" type="submit">Logging in...</button>
{:else}
	<button class="btn mt-4 btn-neutral" type="submit" onclick={() => login()}>Login</button>
{/if}

{#if errorMessage}
	{errorMessage}
{/if}
