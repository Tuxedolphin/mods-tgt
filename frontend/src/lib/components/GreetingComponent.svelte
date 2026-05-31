<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { get_user_info } from '$lib/utils/db_operations';
	import { onMount } from 'svelte';

	interface GreetingComponentProps {
		access_token: string;
	}
	const { access_token }: GreetingComponentProps = $props();
	let username = $state('');
	onMount(async () => {
		const tt = await get_user_info(access_token);

		if (tt.isOk()) {
			if (!tt.value.username) {
				goto(resolve('/newuser'));
			} else {
				username = tt.value.username;
			}
		}
	});
</script>

{#if username}
	<div class="text-xl">Welcome, {username}, glad you could join us.</div>
{:else}
	<div class="h-12 w-full skeleton"></div>
{/if}
