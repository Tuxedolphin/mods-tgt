<script lang="ts">
	import type { LayoutProps } from './$types';
	import mods_tgt_header from '$lib/assets/mods_tgt_header.png?enhanced';
	import { Menu } from '@lucide/svelte';
	// eslint-disable-next-line @typescript-eslint/no-unused-vars
	let { data, children }: LayoutProps = $props();
</script>

<div class="drawer">
	<input id="main-drawer" type="checkbox" class="drawer-toggle" />
	<div class="drawer-content">
		<div class="navbar justify-between gap-1 bg-base-200 shadow-sm">
			<div class="flex items-center">
				<label for="main-drawer" class="drawer-button">
					<Menu></Menu>
				</label>
				<enhanced:img
					class="aspect-5/2 h-14 w-auto align-middle"
					src={mods_tgt_header}
					alt="Mods Together Logo"
				/>
			</div>

			<div class="avatar avatar-placeholder">
				<div class="w-12 rounded-full bg-neutral text-neutral-content">
					<span class="">J</span>
				</div>
			</div>
		</div>

		<div class="px-2 pt-2">
			{@render children()}
		</div>
	</div>

	<div class="drawer-side">
		<label for="main-drawer" aria-label="close sidebar" class="drawer-overlay"></label>
		<ul class="menu min-h-full w-64 bg-base-200 p-4">
			<!-- Sidebar content here -->
			<li><a>Sidebar Item 1</a></li>
			<li><a>Sidebar Item 2</a></li>
		</ul>
	</div>
</div>
