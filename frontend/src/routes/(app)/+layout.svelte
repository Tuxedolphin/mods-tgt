<script lang="ts">
	import { HouseHeart } from '@lucide/svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import mods_tgt_header from '$lib/assets/mods_tgt_header.png?enhanced';
	import type { LayoutProps } from './$types';
	import NavigationBar from './NavigationBar.svelte';

	let { data, children, params }: LayoutProps = $props();
</script>

<div class="drawer">
	<input id="main-drawer" type="checkbox" class="drawer-toggle" />
	<div class="drawer-content">
		<NavigationBar {children} {data} {params}></NavigationBar>
	</div>

	<div class="drawer-side">
		<label for="main-drawer" aria-label="close sidebar" class="drawer-overlay"></label>
		<ul class="menu min-h-full w-64 bg-base-200 p-4">
			<!-- Sidebar content here -->
			<enhanced:img
				class="aspect-5/2 h-14 w-auto align-middle"
				src={mods_tgt_header}
				alt="Mods Together Logo"
			/>
			<li>
				<button onclick={() => goto(resolve('/(app)/home'))}>
					<HouseHeart />
					<span>Home</span>
				</button>
			</li>
			<!-- <li>
				<button>
					<UsersRound />
					<span>Shared with me</span>
				</button>
			</li>

			<li>
				<button>
					<Settings />
					<span>Settings</span>
				</button>
			</li> -->
		</ul>
	</div>
</div>
