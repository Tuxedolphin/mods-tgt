<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { registered } from '$lib/shared/shared.svelte';
	import { register_db } from '$lib/utils/db_operations';

	interface RegisterButtonProps {
		email: string;
		password: string;
	}
	let loading = $state(false);
	let { email, password }: RegisterButtonProps = $props();
	let errorMessage = $state('');

	async function register() {
		loading = true;
		const result = await register_db(email, password);
		if (result.isOk()) {
			registered.set(true);
		} else {
			errorMessage = result.error;
		}
		loading = false;
	}
</script>

{#if loading}
	<button class="disabled btn mt-4 btn-neutral" type="submit">Registering...</button>
{:else}
	<button class="btn mt-4 btn-neutral" type="submit" onclick={() => register()}>Register</button>
{/if}

{#if errorMessage}
	{errorMessage}
{/if}

<button class="btn mt-4" type="submit" onclick={() => goto(resolve('/'))}>Back to Login</button>
