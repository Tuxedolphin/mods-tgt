<script lang="ts">
	import UserAvatar from './UserAvatar.svelte';

	import mods_tgt_header from '$lib/assets/mods_tgt_header.png?enhanced';
	import { Menu } from '@lucide/svelte';
	import type { LayoutProps } from '../$types';
	// eslint-disable-next-line @typescript-eslint/no-unused-vars
	let { children, params }: LayoutProps = $props();

	import { currentUserInformation } from '$lib/shared/shared.svelte';
</script>

<div class="navbar justify-between bg-base-200 shadow-sm">
	<div class="flex items-center">
		<label for="main-drawer" aria-label="open sidebar">
			<Menu></Menu>
		</label>
		<enhanced:img
			class="aspect-5/2 h-14 w-auto align-middle"
			src={mods_tgt_header}
			alt="Mods Together Logo"
		/>
	</div>
	<div class="flex items-center gap-1">
		<p>{$currentUserInformation.username}</p>
		<UserAvatar></UserAvatar>
	</div>
</div>

<div class="px-2 pt-2">
	{@render children()}
</div>
